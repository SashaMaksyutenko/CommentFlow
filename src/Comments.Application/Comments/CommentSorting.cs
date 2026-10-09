using Comments.Application.Common;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public static class CommentSorting
{
    // Works for EF (turned into ORDER BY) and for in-memory lists in tests.
    // Id is a tie-breaker, so the order is stable and pages don't mix up equal values.
    public static IQueryable<Comment> ApplySorting(this IQueryable<Comment> query, CommentSortField sortBy, SortDirection direction)
    {
        var ascending = direction == SortDirection.Asc;

        return sortBy switch
        {
            CommentSortField.UserName => ascending
                ? query.OrderBy(c => c.User.UserName).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.User.UserName).ThenByDescending(c => c.Id),

            CommentSortField.Email => ascending
                ? query.OrderBy(c => c.User.Email).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.User.Email).ThenByDescending(c => c.Id),

            _ => ascending
                ? query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
        };
    }
}
