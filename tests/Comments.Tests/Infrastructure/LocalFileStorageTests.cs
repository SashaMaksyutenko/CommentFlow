using Comments.Infrastructure.Files;

namespace Comments.Tests.Infrastructure;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "comments-tests-" + Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Save_CreatesFolderAndWritesFile()
    {
        await _storage.SaveAsync("a.txt", [1, 2, 3], CancellationToken.None);

        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(_root, "a.txt")));
    }

    [Fact]
    public async Task Delete_RemovesFile_AndIgnoresMissingFile()
    {
        await _storage.SaveAsync("a.txt", [1], CancellationToken.None);

        await _storage.DeleteAsync("a.txt", CancellationToken.None);
        await _storage.DeleteAsync("a.txt", CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(_root, "a.txt")));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("sub/inside.txt")]
    [InlineData("")]
    public async Task NameWithPath_IsRejected(string fileName)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveAsync(fileName, [1], CancellationToken.None));
    }
}
