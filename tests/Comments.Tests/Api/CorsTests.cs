using Microsoft.AspNetCore.Hosting;

namespace Comments.Tests.Api;

// A browser on another address checks the Access-Control-* headers of the answer.
// /api/captcha needs no database, so it shows the headers without a real SQL Server.
public class CorsTests : IClassFixture<ApiFactory>
{
    private const string Frontend = "https://comments.example";

    private readonly ApiFactory _factory;

    public CorsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClientAllowing(string? origin) =>
        (origin is null
            ? _factory
            : _factory.WithWebHostBuilder(builder => builder.UseSetting("Cors:AllowedOrigins:0", origin)))
        .CreateClient();

    // A request the way the browser sends it from a page at the given address
    private static Task<HttpResponseMessage> GetCaptchaFrom(HttpClient client, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/captcha");
        request.Headers.Add("Origin", origin);
        return client.SendAsync(request);
    }

    private static string? AllowedOrigin(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;

    [Fact]
    public async Task AListedFrontend_IsAllowed_WithCredentials()
    {
        var response = await GetCaptchaFrom(ClientAllowing(Frontend), Frontend);

        Assert.Equal(Frontend, AllowedOrigin(response));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task AnotherSite_IsNotAllowed()
    {
        var response = await GetCaptchaFrom(ClientAllowing(Frontend), "https://evil.example");

        Assert.Null(AllowedOrigin(response));
    }

    [Fact]
    public async Task ByDefault_NoOtherAddressIsAllowed()
    {
        var response = await GetCaptchaFrom(ClientAllowing(null), Frontend);

        Assert.Null(AllowedOrigin(response));
    }

    [Fact]
    public async Task ASlashAtTheEndOfTheSetting_DoesNotMatter()
    {
        var response = await GetCaptchaFrom(ClientAllowing(Frontend + "/"), Frontend);

        Assert.Equal(Frontend, AllowedOrigin(response));
    }

    [Fact]
    public async Task TheBrowserCheckBeforeSignalR_IsAnswered()
    {
        // Before connecting, the SignalR client sends POST /negotiate with its own header,
        // so the browser first asks with OPTIONS whether that is allowed
        var request = new HttpRequestMessage(HttpMethod.Options, "/hubs/comments/negotiate?negotiateVersion=1");
        request.Headers.Add("Origin", Frontend);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "x-requested-with,x-signalr-user-agent");

        var response = await ClientAllowing(Frontend).SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(Frontend, AllowedOrigin(response));
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
    }
}
