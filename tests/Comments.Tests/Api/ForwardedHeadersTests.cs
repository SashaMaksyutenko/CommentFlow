using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Comments.Tests.Api;

// The rate limiter counts requests per client IP, so it shows which IP the API sees.
// 203.0.113.x addresses are reserved for examples and documentation.
public class ForwardedHeadersTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ForwardedHeadersTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // One CAPTCHA per minute for every client: the second request of the same client gets 429
    private HttpClient CreateClient(bool forwardedHeadersEnabled) =>
        _factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RateLimits:CaptchaPerMinute", "1");
                builder.UseSetting("ForwardedHeaders:Enabled", forwardedHeadersEnabled ? "true" : "false");
            })
            .CreateClient();

    // A request the way nginx passes it on: the visitor's address is in X-Forwarded-For
    private static Task<HttpResponseMessage> GetCaptchaFrom(HttpClient client, string visitorIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/captcha");
        request.Headers.Add("X-Forwarded-For", visitorIp);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task Enabled_EveryVisitorBehindTheProxyHasOwnLimit()
    {
        var client = CreateClient(forwardedHeadersEnabled: true);

        var first = await GetCaptchaFrom(client, "203.0.113.1");
        var anotherVisitor = await GetCaptchaFrom(client, "203.0.113.2");
        var firstAgain = await GetCaptchaFrom(client, "203.0.113.1");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, anotherVisitor.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, firstAgain.StatusCode);
    }

    [Fact]
    public async Task Disabled_TheHeaderIsIgnored_SoAFakeAddressDoesNotGetAroundTheLimit()
    {
        var client = CreateClient(forwardedHeadersEnabled: false);

        var first = await GetCaptchaFrom(client, "203.0.113.1");
        var pretendingToBeAnother = await GetCaptchaFrom(client, "203.0.113.2");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, pretendingToBeAnother.StatusCode);
    }
}
