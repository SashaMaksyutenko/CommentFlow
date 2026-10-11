using Comments.Domain.Entities;

namespace Comments.Application.Attachments;

public interface IAttachmentRepository
{
    Task<Attachment?> FindAsync(int id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
