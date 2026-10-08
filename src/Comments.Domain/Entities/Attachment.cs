using Comments.Domain.Enums;

namespace Comments.Domain.Entities;

public class Attachment
{
    public const int OriginalFileNameMaxLength = 255;
    public const int StoredFileNameMaxLength = 100;
    public const int ContentTypeMaxLength = 100;

    public Attachment(string originalFileName, string storedFileName, string contentType, long size, AttachmentType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        Size = size;
        Type = type;
    }

    public int Id { get; private set; }

    public int CommentId { get; private set; }

    // Name the user uploaded, only for display
    public string OriginalFileName { get; private set; }

    // Generated name on disk, so users can't control file paths
    public string StoredFileName { get; private set; }

    public string ContentType { get; private set; }

    // In bytes
    public long Size { get; private set; }

    public AttachmentType Type { get; private set; }

    public Comment Comment { get; private set; } = null!;
}
