using Comments.Application.Attachments;
using Comments.Application.Captcha;
using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Application.Users;
using Comments.Application.Validation;
using Comments.Domain.Entities;
using Comments.Domain.Enums;

namespace Comments.Tests.Application;

public class CommentServiceTests
{
    private readonly FakeCaptchaService _captcha = new();
    private readonly FakeUserService _users = new();
    private readonly FakeCommentRepository _comments = new();
    private readonly FakeAttachmentService _attachments = new();
    private readonly FakeCommentCache _cache = new();
    private readonly CommentService _service;

    private static readonly UploadedFile TextFile = new("notes.txt", [72, 105]);

    public CommentServiceTests()
    {
        _service = new CommentService(_captcha, _users, _comments, new CommentTextSanitizer(), _attachments, _cache);
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
    public async Task Create_NewComment_InvalidatesCache()
    {
        await _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None);

        Assert.Equal(1, _cache.InvalidateCount);
    }

    [Fact]
    public async Task Create_Rejected_DoesNotInvalidateCache()
    {
        _captcha.IsValid = false;

        await Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.CreateAsync(ValidRequest(), null, null, null, CancellationToken.None));

        Assert.Equal(0, _cache.InvalidateCount);
    }

    private class FakeCommentCache : ICommentCache
    {
        public PagedResponse<CommentResponse>? PageToReturn { get; set; }

        public PagedResponse<CommentResponse>? StoredPage { get; private set; }

        public GetCommentsQuery? StoredQuery { get; private set; }

        public int InvalidateCount { get; private set; }

        public Task<PagedResponse<CommentResponse>?> GetPageAsync(GetCommentsQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(PageToReturn);

        public Task SetPageAsync(GetCommentsQuery query, PagedResponse<CommentResponse> page, CancellationToken cancellationToken)
        {
            StoredQuery = query;
            StoredPage = page;
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(CancellationToken cancellationToken)
        {
            InvalidateCount++;
            return Task.CompletedTask;
        }
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
            return Task.FromResult(new Attachment(file.FileName, "stored.txt", "text/plain", file.Content.Length, AttachmentType.Text));
        }

        public Task DeleteFileAsync(Attachment attachment, CancellationToken cancellationToken)
        {
            DeleteCount++;
            return Task.CompletedTask;
        }
    }
}
