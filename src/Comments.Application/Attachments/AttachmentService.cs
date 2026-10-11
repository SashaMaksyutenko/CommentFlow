using System.Text;
using Comments.Application.Validation;
using Comments.Domain.Entities;
using Comments.Domain.Enums;

namespace Comments.Application.Attachments;

public class AttachmentService : IAttachmentService
{
    public const string FieldName = "File";
    public const int MaxTextSize = 100 * 1024;
    public const int MaxImageSize = 5 * 1024 * 1024;

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    private readonly IImageProcessor _imageProcessor;
    private readonly IFileStorage _storage;

    public AttachmentService(IImageProcessor imageProcessor, IFileStorage storage)
    {
        _imageProcessor = imageProcessor;
        _storage = storage;
    }

    public async Task<Attachment> SaveAsync(UploadedFile file, CancellationToken cancellationToken)
    {
        if (file.Content.Length == 0)
        {
            throw new FieldValidationException(FieldName, "The file is empty.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var originalName = CleanFileName(file.FileName);

        if (extension == ".txt")
        {
            ValidateText(file.Content);

            // Text needs no processing, it is ready right away
            var storedName = NewStoredName(".txt");
            await _storage.SaveAsync(storedName, file.Content, cancellationToken);

            return new Attachment(originalName, storedName, "text/plain", file.Content.Length, AttachmentType.Text);
        }

        if (ImageExtensions.Contains(extension))
        {
            if (file.Content.Length > MaxImageSize)
            {
                throw new FieldValidationException(FieldName, "Image must be at most 5 MB.");
            }

            // Only the quick check here, so the user gets an error at once for a wrong file.
            // Decoding and resizing is done later by AttachmentProcessingService.
            if (!_imageProcessor.TryInspect(file.Content, out var format, out var error))
            {
                throw new FieldValidationException(FieldName, error);
            }

            // The extension comes from the real format, not from the uploaded name
            var storedName = NewStoredName(format!.Extension);
            await _storage.SaveAsync(PendingUpload.NameFor(storedName), file.Content, cancellationToken);

            return new Attachment(
                originalName, storedName, format.ContentType, file.Content.Length, AttachmentType.Image, AttachmentStatus.Processing);
        }

        throw new FieldValidationException(FieldName, "Only JPG, GIF, PNG images and TXT files are allowed.");
    }

    // Removes both the final file and the waiting original, whichever exists
    public async Task DeleteFileAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        await _storage.DeleteAsync(attachment.StoredFileName, cancellationToken);
        await _storage.DeleteAsync(PendingUpload.NameFor(attachment.StoredFileName), cancellationToken);
    }

    // A real text file: not too big, valid UTF-8 and no zero bytes (those mean binary data)
    private static void ValidateText(byte[] content)
    {
        if (content.Length > MaxTextSize)
        {
            throw new FieldValidationException(FieldName, "Text file must be at most 100 KB.");
        }

        if (content.Contains((byte)0))
        {
            throw new FieldValidationException(FieldName, "The file is not a text file.");
        }

        try
        {
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            throw new FieldValidationException(FieldName, "Text file must be in UTF-8.");
        }
    }

    // Generated name, so the user can't choose the path or overwrite other files
    private static string NewStoredName(string extension)
    {
        return Guid.NewGuid().ToString("N") + extension;
    }

    // Keeps only the name part ("C:\docs\cat.png" -> "cat.png"), the name is only shown to users
    private static string CleanFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/')).Trim();

        if (name.Length == 0)
        {
            name = "file";
        }

        return name.Length <= Attachment.OriginalFileNameMaxLength
            ? name
            : name[^Attachment.OriginalFileNameMaxLength..];
    }
}
