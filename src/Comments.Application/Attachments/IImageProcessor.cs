namespace Comments.Application.Attachments;

public interface IImageProcessor
{
    // Checks that the bytes are a real JPG/GIF/PNG and shrinks it to fit 320x240.
    // Returns false and an error message for anything else.
    bool TryProcess(byte[] content, out ProcessedImage? image, out string error);
}
