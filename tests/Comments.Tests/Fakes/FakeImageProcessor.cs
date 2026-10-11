using Comments.Application.Attachments;

namespace Comments.Tests.Fakes;

// By default every file "is" a PNG and processing always works.
// Set Format or Processed to null to make that part fail.
public class FakeImageProcessor : IImageProcessor
{
    public ImageFormatInfo? Format { get; set; } = new(".png", "image/png");

    public ProcessedImage? Processed { get; set; } = new([1, 2, 3], ".png", "image/png", 320, 240);

    public string Error { get; set; } = "The file is not a JPG, GIF or PNG image.";

    public int InspectCount { get; private set; }

    public int ProcessCount { get; private set; }

    public bool TryInspect(byte[] content, out ImageFormatInfo? format, out string error)
    {
        InspectCount++;
        format = Format;
        error = Format is null ? Error : "";
        return Format is not null;
    }

    public bool TryProcess(byte[] content, out ProcessedImage? image, out string error)
    {
        ProcessCount++;
        image = Processed;
        error = Processed is null ? Error : "";
        return Processed is not null;
    }
}
