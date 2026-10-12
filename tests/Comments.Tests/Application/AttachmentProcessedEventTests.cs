using Comments.Application.Attachments;
using Comments.Application.Attachments.Events;
using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Tests.Api;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Application;

public class AttachmentProcessedEventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AttachmentProcessedEventTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // Uses the real DI container of the API: MediatR must find the handler,
    // and the handler must drop the cached pages (they may still say "Processing")
    [Fact]
    public async Task PublishedThroughMediatR_ClearsCachedPages()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICommentCache>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var query = new GetCommentsQuery();
        await cache.SetPageAsync(query, new PagedResponse<CommentResponse>([], 1, 25, 0), CancellationToken.None);
        Assert.NotNull(await cache.GetPageAsync(query, CancellationToken.None));

        var attachment = new AttachmentResponse("/uploads/a.png", "cat.png", "Image", "Ready", "image/png", 100);
        await publisher.Publish(new AttachmentProcessed(CommentId: 1, attachment));

        Assert.Null(await cache.GetPageAsync(query, CancellationToken.None));
    }
}
