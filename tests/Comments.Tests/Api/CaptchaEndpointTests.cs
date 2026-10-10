using System.Net;
using System.Net.Http.Json;
using Comments.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Comments.Tests.Api;

public class CaptchaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CaptchaEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Captcha doesn't use the DB, any connection string works
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:CommentsDb", "Server=127.0.0.1,1;Database=CommentsDb"));
    }

    [Fact]
    public async Task Get_ReturnsIdAndPngImage_AndIsNotCached()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/captcha");
        var body = await response.Content.ReadFromJsonAsync<CaptchaResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.Id));
        Assert.StartsWith("data:image/png;base64,", body.Image);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Get_TwoRequests_ReturnDifferentIds()
    {
        var client = _factory.CreateClient();

        var first = await client.GetFromJsonAsync<CaptchaResponse>("/api/captcha");
        var second = await client.GetFromJsonAsync<CaptchaResponse>("/api/captcha");

        Assert.NotEqual(first!.Id, second!.Id);
    }

    [Fact]
    public async Task Get_OverTheRateLimit_Returns429()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimits:CaptchaPerMinute", "2"))
            .CreateClient();

        var first = await client.GetAsync("/api/captcha");
        var second = await client.GetAsync("/api/captcha");
        var third = await client.GetAsync("/api/captcha");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
