using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Comments.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly CommentsDbContext _dbContext;

    public CommentRepository(CommentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return _dbContext.Comments.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken)
    {
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Goes down level by level: one query per depth level, until a level has no replies
    public async Task<IReadOnlyList<Comment>> GetAllRepliesAsync(int commentId, CancellationToken cancellationToken)
    {
        var result = new List<Comment>();
        var parentIds = new List<int> { commentId };

        while (parentIds.Count > 0)
        {
            var level = await _dbContext.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.ParentId != null && parentIds.Contains(c.ParentId.Value))
                .ToListAsync(cancellationToken);

            result.AddRange(level);
            parentIds = level.Select(c => c.Id).ToList();
        }

        return result;
    }

    public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetTopLevelPageAsync(
        CommentSortField sortBy,
        SortDirection direction,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        // Read-only query, so EF doesn't need to track the entities
        var topLevel = _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.ParentId == null);

        var totalCount = await topLevel.CountAsync(cancellationToken);

        var items = await topLevel
            .Include(c => c.User)
            .ApplySorting(sortBy, direction)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
