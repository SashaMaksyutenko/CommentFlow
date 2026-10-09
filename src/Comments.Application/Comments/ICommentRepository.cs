using Comments.Application.Common;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public interface ICommentRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(Comment comment, CancellationToken cancellationToken);

    // All replies at any depth under the comment, as a flat list with User loaded
    Task<IReadOnlyList<Comment>> GetAllRepliesAsync(int commentId, CancellationToken cancellationToken);

    // Comments without a parent, with their User loaded
    Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetTopLevelPageAsync(
        CommentSortField sortBy,
        SortDirection direction,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
