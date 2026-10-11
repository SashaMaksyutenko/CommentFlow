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

    public bool TryInspect(byte[] content, out ImageFormatInfo? format, out string error)
    {
        format = null;

        if (!TryCheck(content, out var magickFormat, out error))
        {
            return false;
        }

        format = new ImageFormatInfo(Extension(magickFormat), ContentType(magickFormat));
        return true;
    }

    public bool TryProcess(byte[] content, out ProcessedImage? image, out string error)
    {
        image = null;

        if (!TryCheck(content, out var format, out error))
        {
            return false;
        }

        try
        {
            using var frames = new MagickImageCollection(content, ReadSettings(format));

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
                frames.ToByteArray(format),
                Extension(format),
                ContentType(format),
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

    // The quick part shared by both methods: format by the first bytes and size by the headers
    private static bool TryCheck(byte[] content, out MagickFormat format, out string error)
    {
        error = "";
        format = default;

        var detected = DetectFormat(content);
        if (detected is null)
        {
            error = "The file is not a JPG, GIF or PNG image.";
            return false;
        }

        format = detected.Value;

        try
        {
            if (!HasAllowedSize(content, ReadSettings(format)))
            {
                error = $"Image is too large, max {MaxSourceSide} pixels per side and {MaxTotalPixels / 1_000_000} megapixels in total.";
                return false;
            }
        }
        catch (MagickException)
        {
            error = "The image file is damaged.";
            return false;
        }

        return true;
    }

    // Read with the detected format only. Without this ImageMagick guesses the format
    // itself and could open things like SVG or scripts hidden behind a .png name.
    private static MagickReadSettings ReadSettings(MagickFormat format)
    {
        return new MagickReadSettings { Format = format };
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
