using Comments.Domain.Entities;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Comments.Tests.Infrastructure;

// Runs on SQLite in memory: a real relational database, created fresh for every test
public class DatabaseFileStorageTests : IDisposable
{
    // An in-memory SQLite database lives only while its connection is open
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<CommentsDbContext> _options;
    private readonly DatabaseFileStorage _storage;

    public DatabaseFileStorageTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new CommentsDbContext(_options);
        db.Database.EnsureCreated();

        _storage = new DatabaseFileStorage(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task SaveAsync_ThenReadAsync_GivesTheSameContent()
    {
        await _storage.SaveAsync("pic.png", [1, 2, 3], CancellationToken.None);

        Assert.Equal([1, 2, 3], await _storage.ReadAsync("pic.png", CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_ReturnsNull_ForAMissingFile()
    {
        Assert.Null(await _storage.ReadAsync("missing.png", CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_UnderTheSameName_ReplacesTheContent()
    {
        await _storage.SaveAsync("pic.png", [1, 2, 3], CancellationToken.None);

        await _storage.SaveAsync("pic.png", [9], CancellationToken.None);

        Assert.Equal([9], await _storage.ReadAsync("pic.png", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFile_AndDoesNothingForAMissingOne()
    {
        await _storage.SaveAsync("pic.png", [1, 2, 3], CancellationToken.None);

        await _storage.DeleteAsync("pic.png", CancellationToken.None);
        await _storage.DeleteAsync("missing.png", CancellationToken.None);

        Assert.Null(await _storage.ReadAsync("pic.png", CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_DoesNotSaveOtherChangesOfTheRequest()
    {
        // The request's own DbContext with a user that is not saved yet
        await using var requestDb = new CommentsDbContext(_options);
        requestDb.Users.Add(new User("Sasha1", "sasha@example.com", null));

        await _storage.SaveAsync("pic.png", [1, 2, 3], CancellationToken.None);

        await using var checkDb = new CommentsDbContext(_options);
        Assert.Equal(0, await checkDb.Users.CountAsync());
        Assert.Equal(1, await checkDb.StoredFiles.CountAsync());
    }
}
