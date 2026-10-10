using Comments.Application.Attachments;
using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comments.Tests.Infrastructure;

public class DistributedCommentCacheTests
{
    private readonly DistributedCommentCache _cache = new(
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
        NullLogger<DistributedCommentCache>.Instance);

    private static PagedResponse<CommentResponse> SamplePage()
    {
        var comment = new CommentResponse(7, null, "Sasha1", "sasha@example.com", "https://example.com/",
            "Hi <i>all</i>", new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc))
        {
            Attachment = new AttachmentResponse("/uploads/a.png", "cat.png", "Image", "image/png", 100)
        };

        return new PagedResponse<CommentResponse>([comment], 1, 25, 26);
    }

    [Fact]
    public async Task GetPage_NothingStored_ReturnsNull()
    {
        Assert.Null(await _cache.GetPageAsync(new GetCommentsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task GetPage_AfterSet_ReturnsTheSameData()
    {
        var query = new GetCommentsQuery();
        await _cache.SetPageAsync(query, SamplePage(), CancellationToken.None);

        var page = await _cache.GetPageAsync(query, CancellationToken.None);

        Assert.Equal(26, page!.TotalCount);
        Assert.Equal(2, page.TotalPages);
        var comment = Assert.Single(page.Items);
        Assert.Equal(7, comment.Id);
        Assert.Equal("Hi <i>all</i>", comment.Text);
        Assert.Equal(DateTimeKind.Utc, comment.CreatedAt.Kind);
        Assert.Equal("/uploads/a.png", comment.Attachment!.Url);
    }

    [Fact]
    public async Task GetPage_OtherPageOrSorting_IsNotMixedUp()
    {
        await _cache.SetPageAsync(new GetCommentsQuery(), SamplePage(), CancellationToken.None);

        Assert.Null(await _cache.GetPageAsync(new GetCommentsQuery { Page = 2 }, CancellationToken.None));
        Assert.Null(await _cache.GetPageAsync(new GetCommentsQuery { SortBy = CommentSortField.Email }, CancellationToken.None));
        Assert.Null(await _cache.GetPageAsync(new GetCommentsQuery { SortDirection = SortDirection.Asc }, CancellationToken.None));
    }

    [Fact]
    public async Task Invalidate_MakesAllStoredPagesUnavailable()
    {
        var firstPage = new GetCommentsQuery();
        var secondPage = new GetCommentsQuery { Page = 2 };
        await _cache.SetPageAsync(firstPage, SamplePage(), CancellationToken.None);
        await _cache.SetPageAsync(secondPage, SamplePage(), CancellationToken.None);

        await _cache.InvalidateAsync(CancellationToken.None);

        Assert.Null(await _cache.GetPageAsync(firstPage, CancellationToken.None));
        Assert.Null(await _cache.GetPageAsync(secondPage, CancellationToken.None));
    }

    [Fact]
    public async Task CacheIsDown_NothingThrows_AndReadIsAMiss()
    {
        var cache = new DistributedCommentCache(new BrokenCache(), NullLogger<DistributedCommentCache>.Instance);
        var query = new GetCommentsQuery();

        await cache.SetPageAsync(query, SamplePage(), CancellationToken.None);
        await cache.InvalidateAsync(CancellationToken.None);

        Assert.Null(await cache.GetPageAsync(query, CancellationToken.None));
    }

    // Behaves like Redis that is not reachable
    private class BrokenCache : IDistributedCache
    {
        private static Exception Down() => new InvalidOperationException("Redis is down");

        public byte[]? Get(string key) => throw Down();
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Down();
        public void Refresh(string key) => throw Down();
        public Task RefreshAsync(string key, CancellationToken token = default) => throw Down();
        public void Remove(string key) => throw Down();
        public Task RemoveAsync(string key, CancellationToken token = default) => throw Down();
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Down();
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw Down();
    }
}
