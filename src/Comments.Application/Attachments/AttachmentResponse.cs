using Comments.Domain.Entities;

namespace Comments.Application.Attachments;

public record AttachmentResponse(string Url, string FileName, string Type, string ContentType, long Size)
{
    // Files are served from this path, see the static files setup in Program.cs
    public const string UrlPrefix = "/uploads";

    public static AttachmentResponse From(Attachment attachment)
    {
        return new AttachmentResponse(
            $"{UrlPrefix}/{attachment.StoredFileName}",
            attachment.OriginalFileName,
            attachment.Type.ToString(), // "Image" or "Text", easier for the client than 0/1
            attachment.ContentType,
            attachment.Size);
    }
}
