namespace Comments.Application.Attachments;

// A file from the request, without ASP.NET types (IFormFile stays in the Api layer)
public record UploadedFile(string FileName, byte[] Content);
