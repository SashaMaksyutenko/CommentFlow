namespace Comments.Application.Attachments;

// Extension is taken from the real format, not from the uploaded file name
public record ProcessedImage(byte[] Content, string Extension, string ContentType, int Width, int Height);
