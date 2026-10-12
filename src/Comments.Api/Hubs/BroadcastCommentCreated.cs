using Comments.Application.Comments.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

// Sends every new comment to all connected browsers
public class BroadcastCommentCreated : INotificationHandler<CommentCreated>
{
    private readonly IHubContext<CommentsHub, ICommentsClient> _hub;
    private readonly ILogger<BroadcastCommentCreated> _logger;

    public BroadcastCommentCreated(IHubContext<CommentsHub, ICommentsClient> hub, ILogger<BroadcastCommentCreated> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(CommentCreated notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.All.CommentCreated(notification.Comment);
        }
        catch (Exception ex)
        {
            // The comment is already saved. A failed live update must not turn the request into an error.
            _logger.LogWarning(ex, "Could not broadcast comment {CommentId}", notification.Comment.Id);
        }
    }
}
