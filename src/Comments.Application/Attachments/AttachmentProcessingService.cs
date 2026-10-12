using Comments.Application.Attachments.Events;
using Comments.Domain.Enums;
using MediatR;

namespace Comments.Application.Attachments;

public class AttachmentProcessingService : IAttachmentProcessingService
{
    private readonly IAttachmentRepository _attachments;
    private readonly IFileStorage _storage;
    private readonly IImageProcessor _imageProcessor;
    private readonly IPublisher _publisher;

    public AttachmentProcessingService(
        IAttachmentRepository attachments,
        IFileStorage storage,
        IImageProcessor imageProcessor,
        IPublisher publisher)
    {
        _attachments = attachments;
        _storage = storage;
        _imageProcessor = imageProcessor;
        _publisher = publisher;
    }

    public async Task ProcessAsync(int attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _attachments.FindAsync(attachmentId, cancellationToken);

        // Unknown id, or already processed (e.g. the same request came twice): nothing to do
        if (attachment is null || attachment.Status != AttachmentStatus.Processing)
        {
            return;
        }

        var pendingName = PendingUpload.NameFor(attachment.StoredFileName);
        var original = await _storage.ReadAsync(pendingName, cancellationToken);

        if (original is not null && _imageProcessor.TryProcess(original, out var image, out _))
        {
            await _storage.SaveAsync(attachment.StoredFileName, image!.Content, cancellationToken);
            attachment.MarkReady(image.Content.Length);
        }
        else
        {
            // The original is missing or can't be decoded
            attachment.MarkFailed();
        }

        // Status first, the original last: if saving fails, the original is still there for a retry
        await _attachments.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(pendingName, cancellationToken);

        // Tell everyone interested (cache, later live updates) that the file has its final status
        await _publisher.Publish(
            new AttachmentProcessed(attachment.CommentId, AttachmentResponse.From(attachment)),
            cancellationToken);
    }
}
