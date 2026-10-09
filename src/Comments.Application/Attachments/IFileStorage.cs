namespace Comments.Application.Attachments;

public interface IFileStorage
{
    Task SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken);

    Task DeleteAsync(string fileName, CancellationToken cancellationToken);
}
