using Comments.Application.Attachments;
using Comments.Application.Attachments.Messages;
using Comments.Infrastructure.Messaging;
using Comments.Tests.Api;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Infrastructure;

public class ProcessAttachmentConsumerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProcessAttachmentConsumerTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // Only the consumer, on MassTransit's own in-memory test bus
    [Fact]
    public async Task Consumer_CallsProcessingWithTheIdFromTheMessage()
    {
        var processing = new RecordingProcessingService();
        await using var provider = new ServiceCollection()
            .AddSingleton<IAttachmentProcessingService>(processing)
            .AddMassTransitTestHarness(config => config.AddConsumer<ProcessAttachmentConsumer>())
            .BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(new ProcessAttachment(7));

        Assert.True(await harness.Consumed.Any<ProcessAttachment>());
        Assert.Equal([7], processing.ProcessedIds);
    }

    // The real registration from the API (with the in-memory queue):
    // a published message must reach the consumer in the background
    [Fact]
    public async Task ApiSetup_DeliversPublishedMessageToTheConsumer()
    {
        var processing = new RecordingProcessingService();
        await using var app = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IAttachmentProcessingService>(processing)));
        var bus = app.Services.GetRequiredService<IBus>();

        await bus.Publish(new ProcessAttachment(42));

        // Publish returns at once, the consumer runs later, so wait for it (but not forever)
        var processedId = await processing.FirstCall.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(42, processedId);
    }

    private class RecordingProcessingService : IAttachmentProcessingService
    {
        private readonly TaskCompletionSource<int> _firstCall = new();

        public List<int> ProcessedIds { get; } = [];

        // Completes when ProcessAsync is called for the first time
        public Task<int> FirstCall => _firstCall.Task;

        public Task ProcessAsync(int attachmentId, CancellationToken cancellationToken)
        {
            ProcessedIds.Add(attachmentId);
            _firstCall.TrySetResult(attachmentId);
            return Task.CompletedTask;
        }
    }
}
