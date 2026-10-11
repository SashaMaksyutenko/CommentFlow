namespace Comments.Application.Attachments;

public interface IFileStorage
{
    Task SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken);

    // Returns null if there is no such file
    Task<byte[]?> ReadAsync(string fileName, CancellationToken cancellationToken);

    Task DeleteAsync(string fileName, CancellationToken cancellationToken);
}
