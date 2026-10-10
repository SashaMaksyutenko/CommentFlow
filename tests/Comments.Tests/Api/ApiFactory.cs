using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Comments.Tests.Api;

// Starts the API in memory for tests, without a real SQL Server or Redis
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Nothing listens on port 1, so the DB is always unreachable and fails fast
        builder.UseSetting(
            "ConnectionStrings:CommentsDb",
            "Server=127.0.0.1,1;Database=CommentsDb;User Id=sa;Password=unused;Connect Timeout=1;TrustServerCertificate=True");

        // Empty = the API uses its in-memory cache instead of Redis
        builder.UseSetting("ConnectionStrings:Redis", "");
    }
}
