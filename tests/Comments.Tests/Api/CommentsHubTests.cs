using Comments.Api.Hubs;
using Comments.Application.Attachments;
using Comments.Application.Attachments.Events;
using Comments.Application.Comments;
using Comments.Application.Comments.Events;
using MediatR;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comments.Tests.Api;

public class CommentsHubTests : IClassFixture<ApiFactory>
{
    private static readonly CommentResponse Comment =
        new(15, null, "Sasha1", "sasha@example.com", null, "Hi <i>all</i>", new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

    private static readonly AttachmentResponse Attachment =
        new("/uploads/a.png", "cat.png", "Image", "Ready", "image/png", 100);

    private readonly ApiFactory _factory;

    public CommentsHubTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // A real SignalR client, connected to the API that runs in memory.
    // Long polling is used because the in-memory test server has no real network for WebSockets.
    private HubConnection CreateConnection()
    {
        var server = _factory.Server;

        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, CommentsHub.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }

    // Publishes an event the same way the application code does
    private async Task PublishAsync(INotification notification)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(notification);
    }

    [Fact]
    public async Task NewComment_IsSentToConnectedClients()
    {
        await using var connection = CreateConnection();
        var received = new TaskCompletionSource<CommentResponse>();
        connection.On<CommentResponse>(nameof(ICommentsClient.CommentCreated), comment => received.TrySetResult(comment));
        await connection.StartAsync();

        await PublishAsync(new CommentCreated(Comment));

        var comment = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(15, comment.Id);
        Assert.Equal("Sasha1", comment.UserName);
        Assert.Equal("Hi <i>all</i>", comment.Text);
        Assert.Equal(Comment.CreatedAt, comment.CreatedAt);
    }

    [Fact]
    public async Task ProcessedAttachment_IsSentToConnectedClients()
    {
        await using var connection = CreateConnection();
        var received = new TaskCompletionSource<(int CommentId, AttachmentResponse Attachment)>();
        connection.On<int, AttachmentResponse>(
            nameof(ICommentsClient.AttachmentProcessed),
            (commentId, attachment) => received.TrySetResult((commentId, attachment)));
        await connection.StartAsync();

        await PublishAsync(new AttachmentProcessed(15, Attachment));

        var (commentId, attachment) = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(15, commentId);
        Assert.Equal("Ready", attachment.Status);
        Assert.Equal("/uploads/a.png", attachment.Url);
    }

    [Fact]
    public async Task BroadcastFails_HandlersDoNotThrow()
    {
        var brokenHub = new BrokenHubContext();
        var commentHandler = new BroadcastCommentCreated(brokenHub, NullLogger<BroadcastCommentCreated>.Instance);
        var attachmentHandler = new BroadcastAttachmentProcessed(brokenHub, NullLogger<BroadcastAttachmentProcessed>.Instance);

        // No exception = the comment request would still succeed
        await commentHandler.Handle(new CommentCreated(Comment), CancellationToken.None);
        await attachmentHandler.Handle(new AttachmentProcessed(15, Attachment), CancellationToken.None);
    }

    // Any attempt to send something fails
    private class BrokenHubContext : IHubContext<CommentsHub, ICommentsClient>
    {
        public IHubClients<ICommentsClient> Clients => throw new InvalidOperationException("SignalR is down");

        public IGroupManager Groups => throw new NotSupportedException();
    }
}
