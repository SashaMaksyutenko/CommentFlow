using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Persistence;

// Brings the database schema up to date when the API starts (turned on in Docker).
// SQL Server may still be starting, so a failed try is repeated a few times before giving up.
public static class DatabaseMigrator
{
    private const int Attempts = 10;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(5);

    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // DbContext is "scoped", so it needs its own scope outside of a request
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigrator));

        // Applies only the migrations the database does not have yet; creates the database if there is none
        await RetryAsync(token => db.Database.MigrateAsync(token), Attempts, DelayBetweenAttempts, logger, cancellationToken);
        logger.LogInformation("The database is up to date");
    }

    // Runs the action. If it throws, waits and runs it again, up to the given number of attempts.
    // After the last failed attempt the error goes on and the API does not start.
    public static async Task RetryAsync(
        Func<CancellationToken, Task> action,
        int attempts,
        TimeSpan delay,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await action(cancellationToken);
                return;
            }
            // A stop request (Ctrl+C, docker stop) is not a reason to try again
            catch (Exception exception) when (attempt < attempts && exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    "The database is not ready yet (attempt {Attempt} of {Attempts}): {Message}",
                    attempt, attempts, exception.Message);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
