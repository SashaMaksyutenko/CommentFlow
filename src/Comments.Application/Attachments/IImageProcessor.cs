namespace Comments.Application.Attachments;

public interface IImageProcessor
{
    // Quick check, cheap enough to run inside the request: is it a real JPG/GIF/PNG
    // (by its first bytes) and is it not too big. Reads only the headers, no pixels.
    bool TryInspect(byte[] content, out ImageFormatInfo? format, out string error);

    // The heavy part: decodes the image, removes metadata and shrinks it to fit 320x240.
    // Does the quick check again, so it is safe to call on its own.
    bool TryProcess(byte[] content, out ProcessedImage? image, out string error);
}
