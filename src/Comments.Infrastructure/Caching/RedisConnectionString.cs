namespace Comments.Infrastructure.Caching;

// The Redis client (StackExchange.Redis) wants "host:port,password=...". Some hosts (Render) give
// the address as a URL instead: "redis://user:password@host:6379", or "rediss://..." with TLS.
// The client does not understand URLs, so such a URL is turned into its format here.
public static class RedisConnectionString
{
    private const int DefaultPort = 6379;

    public static string Normalize(string connectionString)
    {
        // Only real URLs. "redis:6379" (the service name in docker-compose) is a host and a port, not a URL.
        var isUrl = connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                    connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
        if (!isUrl)
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var port = uri.Port == -1 ? DefaultPort : uri.Port;
        var parts = new List<string> { $"{uri.Host}:{port}" };

        // "user:password" before "@"; the user may be empty (":password")
        if (uri.UserInfo.Length > 0)
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo[0].Length > 0)
            {
                parts.Add($"user={Uri.UnescapeDataString(userInfo[0])}");
            }

            if (userInfo.Length == 2)
            {
                parts.Add($"password={Uri.UnescapeDataString(userInfo[1])}");
            }
        }

        if (uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("ssl=true");
        }

        return string.Join(",", parts);
    }
}
