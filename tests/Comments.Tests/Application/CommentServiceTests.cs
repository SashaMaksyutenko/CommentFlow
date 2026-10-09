using Comments.Application.Captcha;
using Comments.Application.Comments;
using Comments.Application.Users;
using Comments.Application.Validation;
using Comments.Domain.Entities;

namespace Comments.Tests.Application;

public class CommentServiceTests
{
    private readonly FakeCaptchaService _captcha = new();
    private readonly FakeUserService _users = new();
    private readonly FakeCommentRepository _comments = new();
    private readonly CommentService _service;

    public CommentServiceTests()
    {
        _service = new CommentService(_captcha, _users, _comments);
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
        var result = await _service.CreateAsync(ValidRequest(), "127.0.0.1", "Mozilla/5.0", CancellationToken.None);

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
            _service.CreateAsync(ValidRequest(), null, null, CancellationToken.None));

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
            _service.CreateAsync(request, null, null, CancellationToken.None));

        Assert.Equal(nameof(CreateCommentRequest.ParentId), ex.Field);
        Assert.Empty(_comments.Added);
    }

    [Fact]
    public async Task Create_ReplyToExistingComment_SetsParentId()
    {
        _comments.ExistingIds.Add(5);
        var request = ValidRequest();
        request.ParentId = 5;

        var result = await _service.CreateAsync(request, null, null, CancellationToken.None);

        Assert.Equal(5, result.ParentId);
    }

    [Fact]
    public async Task Create_TextWithHtml_IsEncoded()
    {
        var request = ValidRequest();
        request.Text = "  <script>alert(1)</script>  ";

        var result = await _service.CreateAsync(request, null, null, CancellationToken.None);

        Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt;", result.Text);
    }

    [Fact]
    public async Task Create_LongUserAgent_IsCut()
    {
        var userAgent = new string('a', Comment.UserAgentMaxLength + 100);

        await _service.CreateAsync(ValidRequest(), null, userAgent, CancellationToken.None);

        Assert.Equal(Comment.UserAgentMaxLength, _comments.Added[0].UserAgent!.Length);
    }

    [Fact]
    public async Task Create_EmptyUserAgent_IsSavedAsNull()
    {
        await _service.CreateAsync(ValidRequest(), null, "", CancellationToken.None);

        Assert.Null(_comments.Added[0].UserAgent);
    }

    // Ids are set by the database, in tests we set them through the private setter
    private static T WithId<T>(T entity, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    private class FakeCaptchaService : ICaptchaService
    {
        public bool IsValid { get; set; } = true;

        public Task<CaptchaChallenge> CreateAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ValidateAsync(string captchaId, string answer, CancellationToken cancellationToken) =>
            Task.FromResult(IsValid);
    }

    private class FakeUserService : IUserService
    {
        public const int UserId = 42;

        public int CallCount { get; private set; }

        public Task<User> GetOrCreateAsync(string userName, string email, string? homePage, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(WithId(new User(userName, email, homePage), UserId));
        }
    }

    private class FakeCommentRepository : ICommentRepository
    {
        public List<int> ExistingIds { get; } = [];

        public List<Comment> Added { get; } = [];

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingIds.Contains(id));

        public Task AddAsync(Comment comment, CancellationToken cancellationToken)
        {
            Added.Add(WithId(comment, Added.Count + 1));
            return Task.CompletedTask;
        }
    }
}
