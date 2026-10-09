using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public interface ICommentRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(Comment comment, CancellationToken cancellationToken);
}
