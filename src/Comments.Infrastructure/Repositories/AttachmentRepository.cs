using Comments.Application.Attachments;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

namespace Comments.Infrastructure.Repositories;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly CommentsDbContext _dbContext;

    public AttachmentRepository(CommentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // FindAsync first looks at the entities this DbContext already loaded,
    // so in the same request it returns the very same object without a query
    public async Task<Attachment?> FindAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Attachments.FindAsync([id], cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
