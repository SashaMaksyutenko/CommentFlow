using MediatR;

namespace Comments.Application.Comments.Events;

// Cached pages are outdated after any new comment:
// the list changes, and the author's home page may have changed too
public class InvalidateCommentCacheOnCommentCreated : INotificationHandler<CommentCreated>
{
    private readonly ICommentCache _cache;

    public InvalidateCommentCacheOnCommentCreated(ICommentCache cache)
    {
        _cache = cache;
    }

    public Task Handle(CommentCreated notification, CancellationToken cancellationToken)
    {
        return _cache.InvalidateAsync(cancellationToken);
    }
}
