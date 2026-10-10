using System.Net;
using Comments.Tests.Api;

namespace Comments.Tests;

public class HealthEndpointTests : IClassFixture<ApiFactory>
{
    // ApiFactory points to a DB that is never reachable
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory)
    {
        _factory = factory;
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
