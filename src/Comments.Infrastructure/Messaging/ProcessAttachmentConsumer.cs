using Comments.Application.Attachments;
using Comments.Application.Attachments.Messages;
using MassTransit;

namespace Comments.Infrastructure.Messaging;

// Runs in the background: MassTransit takes a message from the queue and calls Consume.
// If Consume throws, MassTransit retries the message (see the retry setup in DependencyInjection).
public class ProcessAttachmentConsumer : IConsumer<ProcessAttachment>
{
    private readonly IAttachmentProcessingService _processing;

    public ProcessAttachmentConsumer(IAttachmentProcessingService processing)
    {
        _processing = processing;
    }

    public Task Consume(ConsumeContext<ProcessAttachment> context)
    {
        return _processing.ProcessAsync(context.Message.AttachmentId, context.CancellationToken);
    }
}
