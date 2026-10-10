using System.Text.Json;
using Comments.Application.Comments;
using Comments.Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Caching;

// Keeps pages as JSON in IDistributedCache (Redis, or memory when Redis is not configured).
//
// Invalidation works with a "version" value that is part of every page key:
//   page:<version>:<page>:<sortBy>:<direction>
// A new comment writes a new version, so the old pages are simply never read again
// and disappear by themselves when their lifetime ends.
public class DistributedCommentCache : ICommentCache
{
    public static readonly TimeSpan PageLifetime = TimeSpan.FromMinutes(5);

    private const string VersionKey = "comments-version";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCommentCache> _logger;

    public DistributedCommentCache(IDistributedCache cache, ILogger<DistributedCommentCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponse<CommentResponse>?> GetPageAsync(GetCommentsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.GetStringAsync(await PageKeyAsync(query, cancellationToken), cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<PagedResponse<CommentResponse>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            // The cache only makes things faster. If it is down, read from the database.
            _logger.LogWarning(ex, "Comment cache read failed");
            return null;
        }
    }

    public async Task SetPageAsync(GetCommentsQuery query, PagedResponse<CommentResponse> page, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(page, JsonOptions);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PageLifetime };

            await _cache.SetStringAsync(await PageKeyAsync(query, cancellationToken), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Comment cache write failed");
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetStringAsync(VersionKey, NewVersion(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Comment cache invalidation failed");
        }
    }

    private async Task<string> PageKeyAsync(GetCommentsQuery query, CancellationToken cancellationToken)
    {
        var version = await _cache.GetStringAsync(VersionKey, cancellationToken);

        // First use, or Redis dropped the key: start a new version so no old page can match
        if (version is null)
        {
            version = NewVersion();
            await _cache.SetStringAsync(VersionKey, version, cancellationToken);
        }

        return $"page:{version}:{query.Page}:{query.SortBy}:{query.SortDirection}";
    }

    private static string NewVersion() => Guid.NewGuid().ToString("N");
}
