using Comments.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comments.Tests.Infrastructure;

// The retry part of the migrator. Instead of the real migration each test runs
// a small action that counts its calls and fails as many times as the test wants.
public class DatabaseMigratorTests
{
    private int _calls;

    private Func<CancellationToken, Task> FailsTimes(int failures) => _ =>
    {
        _calls++;
        if (_calls <= failures)
        {
            throw new InvalidOperationException("SQL Server is still starting");
        }

        return Task.CompletedTask;
    };

    private Task Retry(Func<CancellationToken, Task> action, int attempts, CancellationToken cancellationToken = default) =>
        DatabaseMigrator.RetryAsync(action, attempts, TimeSpan.Zero, NullLogger.Instance, cancellationToken);

    [Fact]
    public async Task RetryAsync_RunsOnce_WhenTheFirstTryWorks()
    {
        await Retry(FailsTimes(0), attempts: 5);

        Assert.Equal(1, _calls);
    }

    [Fact]
    public async Task RetryAsync_TriesAgain_UntilTheActionWorks()
    {
        await Retry(FailsTimes(2), attempts: 5);

        Assert.Equal(3, _calls);
    }

    [Fact]
    public async Task RetryAsync_GivesUpAfterTheLastAttempt_AndPassesTheErrorOn()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Retry(FailsTimes(10), attempts: 3));

        Assert.Equal("SQL Server is still starting", error.Message);
        Assert.Equal(3, _calls);
    }

    [Fact]
    public async Task RetryAsync_DoesNotTryAgain_WhenTheStartIsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Retry(
            token =>
            {
                _calls++;
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            },
            attempts: 5,
            cancellation.Token));

        Assert.Equal(1, _calls);
    }
}
