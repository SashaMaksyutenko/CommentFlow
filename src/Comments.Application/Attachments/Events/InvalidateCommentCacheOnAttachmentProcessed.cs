using Comments.Application.Comments;
using MediatR;

namespace Comments.Application.Attachments.Events;

// Cached pages may still show this file as "Processing", so they must be read again
public class InvalidateCommentCacheOnAttachmentProcessed : INotificationHandler<AttachmentProcessed>
{
    private readonly ICommentCache _cache;

    public InvalidateCommentCacheOnAttachmentProcessed(ICommentCache cache)
    {
        _cache = cache;
    }

    public Task Handle(AttachmentProcessed notification, CancellationToken cancellationToken)
    {
        return _cache.InvalidateAsync(cancellationToken);
    }
}
