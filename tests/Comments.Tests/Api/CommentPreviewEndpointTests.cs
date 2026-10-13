using System.Net;
using System.Net.Http.Json;
using Comments.Application.Comments;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Tests.Api;

// The real comment service and the real sanitizer work here.
// The test database is unreachable, so a 200 also proves the preview never touches it.
public class CommentPreviewEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CommentPreviewEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Hi <i>all</i> and <strong>you</strong>", "Hi <i>all</i> and <strong>you</strong>")]
    [InlineData("a < b", "a &lt; b")]
    [InlineData("<a href='https://example.com'>link</a>", "<a href=\"https://example.com\">link</a>")]
    public async Task Post_ValidText_Returns200WithTextAsItWouldBeSaved(string text, string expected)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/comments/preview", new { text });
        var preview = await response.Content.ReadFromJsonAsync<CommentPreviewResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, preview!.Text);
    }

    [Theory]
    [InlineData("<b>bold</b>", "not allowed")]
    [InlineData("<script>alert(1)</script>", "not allowed")]
    [InlineData("<i>not closed", "not closed")]
    public async Task Post_BadTags_Returns400WithTextError(string text, string expectedMessagePart)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/comments/preview", new { text });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expectedMessagePart, Assert.Single(problem!.Errors["Text"]));
    }

    [Fact]
    public async Task Post_EmptyText_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/comments/preview", new { text = "" });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors.ContainsKey("Text"));
    }

    [Fact]
    public async Task Post_TextLongerThanLimit_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/comments/preview", new { text = new string('a', 5001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_OverTheRateLimit_Returns429()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimits:PreviewPerMinute", "2"))
            .CreateClient();

        var first = await client.PostAsJsonAsync("/api/comments/preview", new { text = "Hello" });
        var second = await client.PostAsJsonAsync("/api/comments/preview", new { text = "Hello" });
        var third = await client.PostAsJsonAsync("/api/comments/preview", new { text = "Hello" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
