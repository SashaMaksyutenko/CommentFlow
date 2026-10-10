using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Comments.Tests.Api;

public class UploadsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly string _uploads = Path.Combine(Path.GetTempPath(), "comments-uploads-" + Guid.NewGuid().ToString("N"));
    private readonly HttpClient _client;

    public UploadsEndpointTests(WebApplicationFactory<Program> factory)
    {
        Directory.CreateDirectory(_uploads);
        File.WriteAllBytes(Path.Combine(_uploads, "pic.png"), [1, 2, 3]);
        File.WriteAllText(Path.Combine(_uploads, "note.txt"), "Привіт", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(_uploads, "page.html"), "<script>alert(1)</script>");

        // Point the API to a temp folder instead of the real uploads folder
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:CommentsDb", "Server=127.0.0.1,1;Database=CommentsDb");
            builder.UseSetting("FileStorage:RootPath", _uploads);
        }).CreateClient();
    }

    public void Dispose()
    {
        Directory.Delete(_uploads, recursive: true);
    }

    [Fact]
    public async Task Image_IsServedWithItsTypeAndNoSniff()
    {
        var response = await _client.GetAsync("/uploads/pic.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal([1, 2, 3], await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task TextFile_IsServedAsUtf8PlainText()
    {
        var response = await _client.GetAsync("/uploads/note.txt");

        Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        Assert.Equal("Привіт", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/uploads/page.html")]            // exists, but the type is not allowed
    [InlineData("/uploads/missing.png")]
    [InlineData("/uploads/..%2Fappsettings.json")] // can't leave the folder
    public async Task OtherFiles_AreNotServed(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
