using System.Net;
using System.Net.Http.Json;
using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Application.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Api;

public class CommentsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly FakeCommentService _service = new();
    private readonly WebApplicationFactory<Program> _factory;

    public CommentsEndpointTests(WebApplicationFactory<Program> factory)
    {
        // The real service is replaced, so no DB or captcha is needed here
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:CommentsDb", "Server=127.0.0.1,1;Database=CommentsDb");
            builder.ConfigureTestServices(services => services.AddSingleton<ICommentService>(_service));
        });
    }

    private static object ValidBody() => new
    {
        userName = "Sasha1",
        email = "sasha@example.com",
        homePage = "https://example.com",
        text = "Hello",
        captchaId = "id1",
        captchaAnswer = "AB12CD"
    };

    [Fact]
    public async Task Post_ValidRequest_Returns201WithComment()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TestBrowser/1.0");

        var response = await client.PostAsJsonAsync("/api/comments", ValidBody());
        var comment = await response.Content.ReadFromJsonAsync<CommentResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Sasha1", comment!.UserName);
        Assert.Equal("TestBrowser/1.0", _service.LastUserAgent);
    }

    [Fact]
    public async Task Post_InvalidRequest_Returns400WithFieldErrors_AndDoesNotCallService()
    {
        var client = _factory.CreateClient();
        var body = new { userName = "Саша", email = "bad", text = "", captchaId = "id1", captchaAnswer = "AB12CD" };

        var response = await client.PostAsJsonAsync("/api/comments", body);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("userName", problem!.Errors.Keys);
        Assert.Contains("email", problem.Errors.Keys);
        Assert.Contains("text", problem.Errors.Keys);
        Assert.Equal(0, _service.CallCount);
    }

    [Fact]
    public async Task Post_ServiceRejectsField_Returns400WithThatField()
    {
        _service.ErrorToThrow = new FieldValidationException(nameof(CreateCommentRequest.CaptchaAnswer), "Wrong captcha.");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/comments", ValidBody());
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["Wrong captcha."], problem!.Errors["captchaAnswer"]);
    }

    private class FakeCommentService : ICommentService
    {
        public int CallCount { get; private set; }

        public string? LastUserAgent { get; private set; }

        public FieldValidationException? ErrorToThrow { get; set; }

        public Task<CommentResponse> CreateAsync(CreateCommentRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        {
            CallCount++;
            LastUserAgent = userAgent;

            if (ErrorToThrow is not null)
            {
                throw ErrorToThrow;
            }

            return Task.FromResult(new CommentResponse(1, null, request.UserName, request.Email, request.HomePage, request.Text, DateTime.UtcNow));
        }

        public Task<PagedResponse<CommentResponse>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResponse<CommentResponse>([], query.Page, 25, 0));
        }
    }
}
