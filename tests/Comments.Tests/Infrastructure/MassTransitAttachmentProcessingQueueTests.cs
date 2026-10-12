using Comments.Application.Attachments;
using Comments.Application.Attachments.Messages;
using Comments.Infrastructure.Messaging;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comments.Tests.Infrastructure;

public class MassTransitAttachmentProcessingQueueTests
{
    private readonly RecordingProcessingService _processing = new();

    private MassTransitAttachmentProcessingQueue CreateQueue(IPublishEndpoint publishEndpoint) =>
        new(publishEndpoint, _processing, NullLogger<MassTransitAttachmentProcessingQueue>.Instance)
        {
            // Short, so the "queue is down" tests don't wait 5 seconds
            PublishTimeout = TimeSpan.FromMilliseconds(200)
        };

    [Fact]
    public async Task Enqueue_QueueWorks_PublishesTheMessageAndDoesNotProcessHere()
    {
        // Real MassTransit bus, but in memory
        await using var provider = new ServiceCollection().AddMassTransitTestHarness().BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        using var scope = provider.CreateScope();
        var queue = CreateQueue(scope.ServiceProvider.GetRequiredService<IPublishEndpoint>());

        await queue.EnqueueAsync(7, CancellationToken.None);

        Assert.True(await harness.Published.Any<ProcessAttachment>(m => m.Context.Message.AttachmentId == 7));
        Assert.Empty(_processing.ProcessedIds);
    }

    [Fact]
    public async Task Enqueue_PublishFails_ProcessesRightAway()
    {
        var queue = CreateQueue(new FakePublishEndpoint(_ => throw new InvalidOperationException("broker error")));

        await queue.EnqueueAsync(7, CancellationToken.None);

        Assert.Equal([7], _processing.ProcessedIds);
    }

    [Fact]
    public async Task Enqueue_PublishHangs_GivesUpAfterTheTimeoutAndProcessesRightAway()
    {
        // This is how MassTransit behaves when RabbitMQ is down: Publish just waits
        var queue = CreateQueue(new FakePublishEndpoint(token => Task.Delay(Timeout.Infinite, token)));

        await queue.EnqueueAsync(7, CancellationToken.None);

        Assert.Equal([7], _processing.ProcessedIds);
    }

    [Fact]
    public async Task Enqueue_RequestIsCancelled_DoesNotFallBack()
    {
        var queue = CreateQueue(new FakePublishEndpoint(token => Task.Delay(Timeout.Infinite, token)));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.EnqueueAsync(7, cancelled.Token));

        Assert.Empty(_processing.ProcessedIds);
    }

    private class RecordingProcessingService : IAttachmentProcessingService
    {
        public List<int> ProcessedIds { get; } = [];

        public Task ProcessAsync(int attachmentId, CancellationToken cancellationToken)
        {
            ProcessedIds.Add(attachmentId);
            return Task.CompletedTask;
        }
    }

    // Publish(message, token) does what the test wants; the other overloads are not used by our code
    private class FakePublishEndpoint(Func<CancellationToken, Task> onPublish) : IPublishEndpoint
    {
        public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class =>
            onPublish(cancellationToken);

        public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();

        public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();

        public Task Publish(object message, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();

        public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();

        public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }
}
