using Comments.Application.Attachments;
using Comments.Application.Attachments.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Messaging;

public class MassTransitAttachmentProcessingQueue : IAttachmentProcessingQueue
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAttachmentProcessingService _processing;
    private readonly ILogger<MassTransitAttachmentProcessingQueue> _logger;

    public MassTransitAttachmentProcessingQueue(
        IPublishEndpoint publishEndpoint,
        IAttachmentProcessingService processing,
        ILogger<MassTransitAttachmentProcessingQueue> logger)
    {
        _publishEndpoint = publishEndpoint;
        _processing = processing;
        _logger = logger;
    }

    // When RabbitMQ is down, Publish doesn't fail: it waits for the connection to come back.
    // The request must not hang for that, so we wait only this long.
    public TimeSpan PublishTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public async Task EnqueueAsync(int attachmentId, CancellationToken cancellationToken)
    {
        try
        {
            // Cancelled by the timeout or by the caller, whichever comes first
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(PublishTimeout);

            await _publishEndpoint.Publish(new ProcessAttachment(attachmentId), timeout.Token);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The queue is not available. Don't leave the file "Processing" forever:
            // do the work right here, as if there were no queue.
            _logger.LogWarning(ex, "Queue is unavailable, processing attachment {AttachmentId} inside the request", attachmentId);

            await _processing.ProcessAsync(attachmentId, cancellationToken);
        }
    }
}
