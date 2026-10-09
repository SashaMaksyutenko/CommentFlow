using Comments.Application.Attachments;

namespace Comments.Infrastructure.Files;

// Keeps uploaded files in one folder on disk (a Docker volume in production)
public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);
        await File.WriteAllBytesAsync(GetPath(fileName), content, cancellationToken);
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        File.Delete(GetPath(fileName)); // does nothing if the file is not there
        return Task.CompletedTask;
    }

    // Only plain names inside the folder, "../secret.txt" is rejected
    private string GetPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException("File name must not contain a path.", nameof(fileName));
        }

        return Path.Combine(_rootPath, fileName);
    }
}
