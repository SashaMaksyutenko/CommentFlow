using Comments.Application.Comments;
using Comments.Application.Comments.Events;
using Comments.Application.Common;
using Comments.Tests.Api;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Application;

public class CommentCreatedEventTests : IClassFixture<ApiFactory>
{
    private static readonly CommentCreated Event =
        new(new CommentResponse(1, null, "Sasha1", "sasha@example.com", null, "Hi", DateTime.UtcNow));

    private readonly ApiFactory _factory;

    public CommentCreatedEventTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handler_InvalidatesTheCache()
    {
        var cache = new CountingCache();
        var handler = new InvalidateCommentCacheOnCommentCreated(cache);

        await handler.Handle(Event, CancellationToken.None);

        Assert.Equal(1, cache.InvalidateCount);
    }

    // Uses the real DI container of the API: checks that MediatR finds the handler
    // and that publishing the event really clears a cached page
    [Fact]
    public async Task PublishedThroughMediatR_ClearsCachedPages()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICommentCache>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var query = new GetCommentsQuery();
        await cache.SetPageAsync(query, new PagedResponse<CommentResponse>([], 1, 25, 0), CancellationToken.None);
        Assert.NotNull(await cache.GetPageAsync(query, CancellationToken.None));

        await publisher.Publish(Event);

        Assert.Null(await cache.GetPageAsync(query, CancellationToken.None));
    }

    private class CountingCache : ICommentCache
    {
        public int InvalidateCount { get; private set; }

        public Task<PagedResponse<CommentResponse>?> GetPageAsync(GetCommentsQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<PagedResponse<CommentResponse>?>(null);

        public Task SetPageAsync(GetCommentsQuery query, PagedResponse<CommentResponse> page, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task InvalidateAsync(CancellationToken cancellationToken)
        {
            InvalidateCount++;
            return Task.CompletedTask;
        }
    }
}
