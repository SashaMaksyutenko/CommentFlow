using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Api;

public class CacheRegistrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CacheRegistrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void WithoutRedisConnectionString_CacheIsInMemory()
    {
        var cache = _factory.Services.GetRequiredService<IDistributedCache>();

        Assert.IsType<MemoryDistributedCache>(cache);
    }

    [Fact]
    public void WithRedisConnectionString_CacheIsRedis()
    {
        // Only checks which class is registered; Redis connects on first use, not here
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Redis", "localhost:6379"));

        var cache = factory.Services.GetRequiredService<IDistributedCache>();

        Assert.IsAssignableFrom<RedisCache>(cache);
    }
}
