using Comments.Domain.Entities;

namespace Comments.Application.Attachments;

public interface IAttachmentService
{
    // Validates and stores the file. Throws FieldValidationException if the file is not allowed.
    Task<Attachment> SaveAsync(UploadedFile file, CancellationToken cancellationToken);
}
