using Comments.Application.Common;

namespace Comments.Application.Comments;

// Cache for the pages of top-level comments
public interface ICommentCache
{
    // Returns null if the page is not in the cache
    Task<PagedResponse<CommentResponse>?> GetPageAsync(GetCommentsQuery query, CancellationToken cancellationToken);

    Task SetPageAsync(GetCommentsQuery query, PagedResponse<CommentResponse> page, CancellationToken cancellationToken);

    // Makes all cached pages outdated, called when a new comment is added
    Task InvalidateAsync(CancellationToken cancellationToken);
}
