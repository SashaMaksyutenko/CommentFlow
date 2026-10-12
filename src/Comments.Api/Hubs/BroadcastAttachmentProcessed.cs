using Comments.Application.Attachments.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

// Tells all connected browsers that a file is ready (or failed),
// so they can replace the "processing" placeholder without reloading
public class BroadcastAttachmentProcessed : INotificationHandler<AttachmentProcessed>
{
    private readonly IHubContext<CommentsHub, ICommentsClient> _hub;
    private readonly ILogger<BroadcastAttachmentProcessed> _logger;

    public BroadcastAttachmentProcessed(IHubContext<CommentsHub, ICommentsClient> hub, ILogger<BroadcastAttachmentProcessed> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(AttachmentProcessed notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.All.AttachmentProcessed(notification.CommentId, notification.Attachment);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not broadcast attachment status for comment {CommentId}", notification.CommentId);
        }
    }
}
