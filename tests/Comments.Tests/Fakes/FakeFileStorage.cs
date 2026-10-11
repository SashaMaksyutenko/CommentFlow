using Comments.Application.Attachments;

namespace Comments.Tests.Fakes;

// Keeps "files" in a dictionary instead of the disk
public class FakeFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public Task SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        Files[fileName] = content;
        return Task.CompletedTask;
    }

    public Task<byte[]?> ReadAsync(string fileName, CancellationToken cancellationToken) =>
        Task.FromResult(Files.GetValueOrDefault(fileName));

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        Files.Remove(fileName);
        return Task.CompletedTask;
    }
}
