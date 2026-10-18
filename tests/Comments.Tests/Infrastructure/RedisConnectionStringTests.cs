using Comments.Infrastructure.Caching;

namespace Comments.Tests.Infrastructure;

public class RedisConnectionStringTests
{
    [Theory]
    [InlineData("localhost:6379")]
    [InlineData("redis:6379")]                    // the service name in docker-compose, not a URL
    [InlineData("red-abc:6379,password=secret")]
    public void ClientFormat_StaysAsItIs(string connectionString)
    {
        Assert.Equal(connectionString, RedisConnectionString.Normalize(connectionString));
    }

    [Theory]
    [InlineData("redis://red-abc:6379", "red-abc:6379")]  // what Render gives for its Key Value
    [InlineData("redis://red-abc", "red-abc:6379")]       // no port: the usual Redis port
    [InlineData("redis://:secret@red-abc:6379", "red-abc:6379,password=secret")]
    public void RedisUrl_IsTurnedIntoClientFormat(string url, string expected)
    {
        Assert.Equal(expected, RedisConnectionString.Normalize(url));
    }

    [Fact]
    public void SecureUrl_WithUserAndEncodedPassword_KeepsAllParts()
    {
        var result = RedisConnectionString.Normalize("rediss://default:p%40ss@cache.example:6380");

        Assert.Equal("cache.example:6380,user=default,password=p@ss,ssl=true", result);
    }
}
