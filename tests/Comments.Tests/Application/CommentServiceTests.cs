using Comments.Application.Attachments;
using Comments.Application.Captcha;
using Comments.Application.Comments;
using Comments.Application.Comments.Events;
using Comments.Application.Common;
using Comments.Application.Users;
using Comments.Application.Validation;
using Comments.Domain.Entities;
using Comments.Domain.Enums;
using Comments.Tests.Fakes;

namespace Comments.Tests.Application;

public class CommentServiceTests
{
    private readonly FakeCaptchaService _captcha = new();
    private readonly FakeUserService _users = new();
    private readonly FakeCommentRepository _comments = new();
    private readonly FakeAttachmentService _attachments = new();
    private readonly FakeProcessingQueue _queue = new();
    private readonly FakeCommentCache _cache = new();
    private readonly FakePublisher _publisher = new();
    private readonly CommentService _service;

    private static readonly UploadedFile TextFile = new("notes.txt", [72, 105]);
    private static readonly UploadedFile ImageFile = new("photo.png", [9, 9, 9]);

    public CommentServiceTests()
    {
        _service = new CommentService(_captcha, _users, _comments, new CommentTextSanitizer(), _attachments, _queue, _cache, _publisher);
    }

    private static CreateCommentRequest ValidRequest() => new()
    {
        UserName = "Sasha1",
        Email = "sasha@example.com",
        HomePage = "https://example.com",
        Text = "Hello",
        CaptchaId = "id1",
        CaptchaAnswer = "AB12CD"
    };

    [Fact]
    public async Task Create_ValidRequest_SavesCommentAndReturnsIt()
    {
        var result = await _service.CreateAsync(ValidRequest(), null, "127.0.0.1", "Mozilla/5.0", CancellationToken.None);

        var saved = Assert.Single(_comments.Added);
        Assert.Equal(FakeUserService.UserId, saved.UserId);
        Assert.Null(saved.ParentId);
        Assert.Equal("Hello", saved.Text);
        Assert.Equal("127.0.0.1", saved.IpAddress);
        Assert.Equal("Mozilla/5.0", saved.UserAgent);

        Assert.Equal(saved.Id, result.Id);
        Assert.Equal("Sasha1", result.UserName);
        Assert.Equal("sasha@example.com", result.Email);
        Assert.Equal("https://example.com", result.HomePage);
    }

    [Fact]
    public async Task Create_WrongCaptcha_ThrowsAndSavesNothing()
    {
        _captcha.IsValid = false;

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None));

        Assert.Equal(nameof(CreateCommentRequest.CaptchaAnswer), ex.Field);
        Assert.Empty(_comments.Added);
        Assert.Equal(0, _users.CallCount);
    }

    [Fact]
    public async Task Create_ReplyToMissingComment_Throws()
    {
        var request = ValidRequest();
        request.ParentId = 999;

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(request, null, null, null, CancellationToken.None));

        Assert.Equal(nameof(CreateCommentRequest.ParentId), ex.Field);
        Assert.Empty(_comments.Added);
    }

    [Fact]
    public async Task Create_ReplyToExistingComment_SetsParentId()
    {
        _comments.ExistingIds.Add(5);
        var request = ValidRequest();
        request.ParentId = 5;

        var result = await _service.CreateAsync(request, null, null, null, CancellationToken.None);

        Assert.Equal(5, result.ParentId);
    }

    [Fact]
    public async Task Create_TextWithAllowedTags_IsSavedWithTags()
    {
        var request = ValidRequest();
        request.Text = "Hi <strong>all</strong> & <a href=\"https://example.com\">link</a>";

        var result = await _service.CreateAsync(request, null, null, null, CancellationToken.None);

        Assert.Equal("Hi <strong>all</strong> &amp; <a href=\"https://example.com\">link</a>", result.Text);
    }

    [Fact]
    public async Task Create_TextWithForbiddenTag_ThrowsWithoutUsingCaptcha()
    {
        var request = ValidRequest();
        request.Text = "<script>alert(1)</script>";

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(request, null, null, null, CancellationToken.None));

        Assert.Equal(nameof(CreateCommentRequest.Text), ex.Field);
        Assert.Equal(0, _captcha.CallCount);
        Assert.Empty(_comments.Added);
    }

    [Fact]
    public async Task Create_LongUserAgent_IsCut()
    {
        var userAgent = new string('a', Comment.UserAgentMaxLength + 100);

        await _service.CreateAsync(ValidRequest(), null, null, userAgent, CancellationToken.None);

        Assert.Equal(Comment.UserAgentMaxLength, _comments.Added[0].UserAgent!.Length);
    }

    [Fact]
    public async Task Create_EmptyUserAgent_IsSavedAsNull()
    {
        await _service.CreateAsync(ValidRequest(), null, null, "", CancellationToken.None);

        Assert.Null(_comments.Added[0].UserAgent);
    }

    [Fact]
    public async Task GetTopLevel_SecondPage_SkipsFirst25AndReturnsPageInfo()
    {
        var user = TestData.CreateUser(1, "Sasha1", "sasha@example.com");
        _comments.Page = [TestData.CreateTopLevelComment(30, user, DateTime.UtcNow)];
        _comments.TotalCount = 30;
        var query = new GetCommentsQuery { Page = 2, SortBy = CommentSortField.Email, SortDirection = SortDirection.Asc };

        var result = await _service.GetTopLevelAsync(query, CancellationToken.None);

        Assert.Equal(25, _comments.LastSkip);
        Assert.Equal(25, _comments.LastTake);
        Assert.Equal(CommentSortField.Email, _comments.LastSortBy);
        Assert.Equal(SortDirection.Asc, _comments.LastDirection);

        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(30, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        var item = Assert.Single(result.Items);
        Assert.Equal(30, item.Id);
        Assert.Equal("Sasha1", item.UserName);
    }

    [Fact]
    public async Task Create_WithFile_SavesFileAndAttachesIt()
    {
        var result = await _service.CreateAsync(ValidRequest(), TextFile, null, null, CancellationToken.None);

        var saved = Assert.Single(_comments.Added);
        Assert.NotNull(saved.Attachment);
        Assert.Equal("notes.txt", saved.Attachment.OriginalFileName);

        Assert.Equal("/uploads/stored.txt", result.Attachment!.Url);
        Assert.Equal("notes.txt", result.Attachment.FileName);
        Assert.Equal("Text", result.Attachment.Type);
        Assert.Equal("Ready", result.Attachment.Status);
        Assert.Equal("text/plain", result.Attachment.ContentType);
        Assert.Equal(2, result.Attachment.Size);
    }

    [Fact]
    public async Task GetTopLevel_CommentWithFile_ReturnsAttachment()
    {
        var user = TestData.CreateUser(1, "Sasha1", "sasha@example.com");
        var comment = TestData.CreateTopLevelComment(1, user, DateTime.UtcNow);
        comment.Attach(new Attachment("cat.png", "abc.png", "image/png", 100, AttachmentType.Image));
        _comments.Page = [comment];
        _comments.TotalCount = 1;

        var result = await _service.GetTopLevelAsync(new GetCommentsQuery(), CancellationToken.None);

        var attachment = result.Items[0].Attachment!;
        Assert.Equal("/uploads/abc.png", attachment.Url);
        Assert.Equal("Image", attachment.Type);
    }

    [Fact]
    public async Task Create_WithImage_QueuesItAfterTheCommentIsSaved()
    {
        var savedCommentsWhenQueued = -1;
        _queue.OnEnqueue = () => savedCommentsWhenQueued = _comments.Added.Count;

        await _service.CreateAsync(ValidRequest(), ImageFile, null, null, CancellationToken.None);

        Assert.Equal([FakeAttachmentService.SavedAttachmentId], _queue.EnqueuedIds);

        // The consumer looks the attachment up in the database, so it must be saved first
        Assert.Equal(1, savedCommentsWhenQueued);
    }

    [Fact]
    public async Task Create_WithImage_AnswersWithoutWaitingForProcessing()
    {
        var result = await _service.CreateAsync(ValidRequest(), ImageFile, null, null, CancellationToken.None);

        Assert.Equal("Processing", result.Attachment!.Status);
    }

    [Fact]
    public async Task Create_WithTextFile_QueuesNothing()
    {
        var result = await _service.CreateAsync(ValidRequest(), TextFile, null, null, CancellationToken.None);

        Assert.Empty(_queue.EnqueuedIds);
        Assert.Equal("Ready", result.Attachment!.Status);
    }

    [Fact]
    public async Task Create_WithImage_DatabaseFails_QueuesNothing()
    {
        _comments.ThrowOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(ValidRequest(), ImageFile, null, null, CancellationToken.None));

        Assert.Empty(_queue.EnqueuedIds);
        Assert.Equal(1, _attachments.DeleteCount);
    }

    [Fact]
    public async Task Create_WithoutFile_HasNoAttachment()
    {
        await _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None);

        Assert.Null(_comments.Added[0].Attachment);
        Assert.Equal(0, _attachments.SaveCount);
    }

    [Fact]
    public async Task Create_WrongCaptcha_DoesNotTouchTheFile()
    {
        _captcha.IsValid = false;

        await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(ValidRequest(), TextFile, null, null, CancellationToken.None));

        Assert.Equal(0, _attachments.SaveCount);
    }

    [Fact]
    public async Task Create_BadFile_ThrowsBeforeCreatingUser()
    {
        _attachments.ErrorToThrow = new FieldValidationException(AttachmentService.FieldName, "Only JPG, GIF, PNG images and TXT files are allowed.");

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(ValidRequest(), new UploadedFile("virus.exe", [1]), null, null, CancellationToken.None));

        Assert.Equal(AttachmentService.FieldName, ex.Field);
        Assert.Equal(0, _users.CallCount);
        Assert.Empty(_comments.Added);
    }

    [Fact]
    public async Task Create_DatabaseFails_DeletesStoredFile()
    {
        _comments.ThrowOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(ValidRequest(), TextFile, null, null, CancellationToken.None));

        Assert.Equal(1, _attachments.DeleteCount);
    }

    [Fact]
    public async Task GetReplies_CommentDoesNotExist_ReturnsNull()
    {
        Assert.Null(await _service.GetRepliesAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task GetReplies_ExistingComment_ReturnsTree()
    {
        var user = TestData.CreateUser(1, "Sasha1", "sasha@example.com");
        _comments.ExistingIds.Add(1);
        _comments.Replies =
        [
            TestData.CreateComment(2, 1, user, DateTime.UtcNow),
            TestData.CreateComment(3, 2, user, DateTime.UtcNow.AddMinutes(1))
        ];

        var tree = await _service.GetRepliesAsync(1, CancellationToken.None);

        var reply = Assert.Single(tree!);
        Assert.Equal(2, reply.Id);
        Assert.Equal(3, Assert.Single(reply.Replies).Id);
    }

    [Fact]
    public async Task GetTopLevel_DefaultQuery_IsFirstPageNewestFirst()
    {
        await _service.GetTopLevelAsync(new GetCommentsQuery(), CancellationToken.None);

        Assert.Equal(0, _comments.LastSkip);
        Assert.Equal(CommentSortField.CreatedAt, _comments.LastSortBy);
        Assert.Equal(SortDirection.Desc, _comments.LastDirection);
    }

    [Fact]
    public async Task GetTopLevel_PageNotInCache_ReadsDatabaseAndStoresPage()
    {
        var query = new GetCommentsQuery();

        var result = await _service.GetTopLevelAsync(query, CancellationToken.None);

        Assert.Equal(0, _comments.LastSkip); // the repository was called
        Assert.Same(result, _cache.StoredPage);
        Assert.Same(query, _cache.StoredQuery);
    }

    [Fact]
    public async Task GetTopLevel_PageInCache_IsReturnedWithoutDatabase()
    {
        var cachedPage = new PagedResponse<CommentResponse>([], 1, 25, 0);
        _cache.PageToReturn = cachedPage;

        var result = await _service.GetTopLevelAsync(new GetCommentsQuery(), CancellationToken.None);

        Assert.Same(cachedPage, result);
        Assert.Equal(-1, _comments.LastSkip); // the repository was not called
    }

    [Fact]
    public async Task Create_NewComment_PublishesCommentCreatedWithTheSavedComment()
    {
        var result = await _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None);

        var published = Assert.IsType<CommentCreated>(Assert.Single(_publisher.Published));
        Assert.Same(result, published.Comment);
        Assert.Equal(_comments.Added[0].Id, published.Comment.Id);
    }

    [Fact]
    public async Task Create_Rejected_PublishesNothing()
    {
        _captcha.IsValid = false;

        await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None));

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Create_DatabaseFails_PublishesNothing()
    {
        _comments.ThrowOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None));

        Assert.Empty(_publisher.Published);
    }

    private class FakeCommentCache : ICommentCache
    {
        public PagedResponse<CommentResponse>? PageToReturn { get; set; }

        public PagedResponse<CommentResponse>? StoredPage { get; private set; }

        public GetCommentsQuery? StoredQuery { get; private set; }

        public Task<PagedResponse<CommentResponse>?> GetPageAsync(GetCommentsQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(PageToReturn);

        public Task SetPageAsync(GetCommentsQuery query, PagedResponse<CommentResponse> page, CancellationToken cancellationToken)
        {
            StoredQuery = query;
            StoredPage = page;
            return Task.CompletedTask;
        }

        // The service doesn't call it any more, the CommentCreated handler does
        public Task InvalidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class FakeCaptchaService : ICaptchaService
    {
        public bool IsValid { get; set; } = true;

        public int CallCount { get; private set; }

        public Task<CaptchaChallenge> CreateAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ValidateAsync(string captchaId, string answer, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(IsValid);
        }
    }

    private class FakeUserService : IUserService
    {
        public const int UserId = 42;

        public int CallCount { get; private set; }

        public Task<User> GetOrCreateAsync(string userName, string email, string? homePage, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new User(userName, email, homePage).WithId(UserId));
        }
    }

    private class FakeCommentRepository : ICommentRepository
    {
        public List<int> ExistingIds { get; } = [];

        public List<Comment> Added { get; } = [];

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingIds.Contains(id));

        public bool ThrowOnAdd { get; set; }

        public Task AddAsync(Comment comment, CancellationToken cancellationToken)
        {
            if (ThrowOnAdd)
            {
                throw new InvalidOperationException("DB is down");
            }

            Added.Add(comment.WithId(Added.Count + 1));
            return Task.CompletedTask;
        }

        public List<Comment> Replies { get; set; } = [];

        public Task<IReadOnlyList<Comment>> GetAllRepliesAsync(int commentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Comment>>(Replies);

        public List<Comment> Page { get; set; } = [];

        public int TotalCount { get; set; }

        public int LastSkip { get; private set; } = -1;

        public int LastTake { get; private set; } = -1;

        public CommentSortField LastSortBy { get; private set; }

        public SortDirection LastDirection { get; private set; }

        public Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetTopLevelPageAsync(
            CommentSortField sortBy, SortDirection direction, int skip, int take, CancellationToken cancellationToken)
        {
            LastSortBy = sortBy;
            LastDirection = direction;
            LastSkip = skip;
            LastTake = take;
            return Task.FromResult<(IReadOnlyList<Comment>, int)>((Page, TotalCount));
        }
    }

    private class FakeAttachmentService : IAttachmentService
    {
        public FieldValidationException? ErrorToThrow { get; set; }

        public int SaveCount { get; private set; }

        public int DeleteCount { get; private set; }

        public Task<Attachment> SaveAsync(UploadedFile file, CancellationToken cancellationToken)
        {
            if (ErrorToThrow is not null)
            {
                throw ErrorToThrow;
            }

            SaveCount++;

            // Like the real service: text is ready at once, an image waits for processing
            Last = file.FileName.EndsWith(".txt")
                ? new Attachment(file.FileName, "stored.txt", "text/plain", file.Content.Length, AttachmentType.Text)
                : new Attachment(file.FileName, "stored.png", "image/png", file.Content.Length, AttachmentType.Image, AttachmentStatus.Processing);

            return Task.FromResult(Last.WithId(SavedAttachmentId));
        }

        public const int SavedAttachmentId = 7;

        public Attachment? Last { get; private set; }

        public Task DeleteFileAsync(Attachment attachment, CancellationToken cancellationToken)
        {
            DeleteCount++;
            return Task.CompletedTask;
        }
    }

    // Only remembers what was queued, nothing is processed
    private class FakeProcessingQueue : IAttachmentProcessingQueue
    {
        public List<int> EnqueuedIds { get; } = [];

        // Lets a test look around at the moment of queuing
        public Action? OnEnqueue { get; set; }

        public Task EnqueueAsync(int attachmentId, CancellationToken cancellationToken)
        {
            EnqueuedIds.Add(attachmentId);
            OnEnqueue?.Invoke();
            return Task.CompletedTask;
        }
    }
}
