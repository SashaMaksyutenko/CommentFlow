using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Comments.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Port 1 has no SQL Server listening, so the database is always unreachable
        // and tests do not depend on a running SQL Server.
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting(
                "ConnectionStrings:CommentsDb",
                "Server=127.0.0.1,1;Database=CommentsDb;User Id=sa;Password=unused;Connect Timeout=1;TrustServerCertificate=True"));
    }

    [Fact]
    public async Task Get_Health_ReturnsOkWithHealthyStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Get_HealthReady_WhenDatabaseIsUnreachable_ReturnsServiceUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", body);
    }
}
