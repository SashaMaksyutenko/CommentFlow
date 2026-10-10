using Comments.Application.Attachments;
using ImageMagick;

namespace Comments.Infrastructure.Images;

public class MagickImageProcessor : IImageProcessor
{
    public const int MaxWidth = 320;
    public const int MaxHeight = 240;

    // Bigger pictures could eat all memory while decoding ("image bomb").
    // A decoded pixel takes 4 bytes, so 25 megapixels is about 100 MB of memory.
    public const int MaxSourceSide = 8000;
    public const int MaxTotalPixels = 25_000_000;

    public bool TryProcess(byte[] content, out ProcessedImage? image, out string error)
    {
        image = null;
        error = "";

        var format = DetectFormat(content);
        if (format is null)
        {
            error = "The file is not a JPG, GIF or PNG image.";
            return false;
        }

        // Read with the detected format only. Without this ImageMagick guesses the format
        // itself and could open things like SVG or scripts hidden behind a .png name.
        var settings = new MagickReadSettings { Format = format.Value };

        try
        {
            if (!HasAllowedSize(content, settings))
            {
                error = $"Image is too large, max {MaxSourceSide} pixels per side and {MaxTotalPixels / 1_000_000} megapixels in total.";
                return false;
            }

            using var frames = new MagickImageCollection(content, settings);

            // GIF frames can be partial; make every frame full size before resizing
            if (format == MagickFormat.Gif)
            {
                frames.Coalesce();
            }

            foreach (var frame in frames)
            {
                frame.AutoOrient(); // phone photos store rotation in EXIF
                frame.Strip();      // removes EXIF (GPS location etc.) and other metadata

                if (frame.Width > MaxWidth || frame.Height > MaxHeight)
                {
                    // Fits inside 320x240 and keeps the proportions
                    frame.Resize(new MagickGeometry(MaxWidth, MaxHeight));
                }
            }

            var first = frames[0];
            image = new ProcessedImage(
                frames.ToByteArray(format.Value),
                Extension(format.Value),
                ContentType(format.Value),
                (int)first.Width,
                (int)first.Height);
            return true;
        }
        catch (MagickException)
        {
            error = "The image file is damaged.";
            return false;
        }
    }

    // Ping reads only the headers (sizes), not the pixels, so it is cheap even for a huge file.
    // All frames are counted: an animated GIF needs memory for every frame.
    private static bool HasAllowedSize(byte[] content, MagickReadSettings settings)
    {
        using var headers = new MagickImageCollection();
        headers.Ping(content, settings);

        long totalPixels = 0;
        foreach (var frame in headers)
        {
            // A GIF frame can be smaller than the whole picture, Page is the full canvas
            var width = Math.Max(frame.Width, frame.Page.Width);
            var height = Math.Max(frame.Height, frame.Page.Height);

            if (width > MaxSourceSide || height > MaxSourceSide)
            {
                return false;
            }

            totalPixels += (long)width * height;
        }

        return totalPixels <= MaxTotalPixels;
    }

    // Checks the first bytes ("magic numbers") instead of trusting the file extension
    private static MagickFormat? DetectFormat(byte[] content)
    {
        if (StartsWith(content, 0xFF, 0xD8, 0xFF))
        {
            return MagickFormat.Jpeg;
        }

        if (StartsWith(content, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
        {
            return MagickFormat.Png;
        }

        // "GIF87a" or "GIF89a"
        if (StartsWith(content, 0x47, 0x49, 0x46, 0x38) && content.Length > 5
            && (content[4] == 0x37 || content[4] == 0x39) && content[5] == 0x61)
        {
            return MagickFormat.Gif;
        }

        return null;
    }

    private static bool StartsWith(byte[] content, params byte[] prefix)
    {
        return content.AsSpan().StartsWith(prefix);
    }

    private static string Extension(MagickFormat format) => format switch
    {
        MagickFormat.Jpeg => ".jpg",
        MagickFormat.Png => ".png",
        _ => ".gif"
    };

    private static string ContentType(MagickFormat format) => format switch
    {
        MagickFormat.Jpeg => "image/jpeg",
        MagickFormat.Png => "image/png",
        _ => "image/gif"
    };
}
