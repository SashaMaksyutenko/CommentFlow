namespace Comments.Application.Attachments;

public interface IAttachmentProcessingService
{
    // Turns the uploaded original into the final file and sets the status to Ready or Failed.
    // Safe to call twice for the same id: the second call does nothing.
    Task ProcessAsync(int attachmentId, CancellationToken cancellationToken);
}
