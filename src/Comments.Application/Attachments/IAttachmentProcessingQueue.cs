namespace Comments.Application.Attachments;

public interface IAttachmentProcessingQueue
{
    // Asks to process the attachment in the background and returns without waiting for it
    Task EnqueueAsync(int attachmentId, CancellationToken cancellationToken);
}
