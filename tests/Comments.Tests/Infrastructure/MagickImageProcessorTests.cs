using System.Text;
using Comments.Infrastructure.Images;
using ImageMagick;

namespace Comments.Tests.Infrastructure;

public class MagickImageProcessorTests
{
    private readonly MagickImageProcessor _processor = new();

    private static byte[] CreateImage(MagickFormat format, uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.SteelBlue, width, height);
        return image.ToByteArray(format);
    }

    [Theory]
    [InlineData(800, 600, 320, 240)]   // same proportions
    [InlineData(1000, 250, 320, 80)]   // wide
    [InlineData(300, 600, 120, 240)]   // tall
    [InlineData(100, 50, 100, 50)]     // already small, not enlarged
    public void Png_IsShrunkToFit320x240_KeepingProportions(int width, int height, int expectedWidth, int expectedHeight)
    {
        var content = CreateImage(MagickFormat.Png, (uint)width, (uint)height);

        Assert.True(_processor.TryProcess(content, out var image, out var error), error);

        Assert.Equal(expectedWidth, image!.Width);
        Assert.Equal(expectedHeight, image.Height);
        var info = new MagickImageInfo(image.Content);
        Assert.Equal((uint)expectedWidth, info.Width);
        Assert.Equal(".png", image.Extension);
        Assert.Equal("image/png", image.ContentType);
    }

    [Fact]
    public void Jpeg_IsDetectedAndMetadataIsRemoved()
    {
        using var source = new MagickImage(MagickColors.Orange, 640, 480);
        source.Comment = "secret note";
        var content = source.ToByteArray(MagickFormat.Jpeg);

        Assert.True(_processor.TryProcess(content, out var image, out var error), error);

        Assert.Equal(".jpg", image!.Extension);
        Assert.Equal("image/jpeg", image.ContentType);
        using var result = new MagickImage(image.Content);
        Assert.Null(result.Comment);
    }

    [Fact]
    public void AnimatedGif_KeepsAllFrames_AndResizesEachOne()
    {
        using var source = new MagickImageCollection();
        foreach (var color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            source.Add(new MagickImage(color, 640, 480) { AnimationDelay = 20 });
        }
        var content = source.ToByteArray(MagickFormat.Gif);

        Assert.True(_processor.TryProcess(content, out var image, out var error), error);

        Assert.Equal(".gif", image!.Extension);
        using var result = new MagickImageCollection(image.Content);
        Assert.Equal(3, result.Count);
        Assert.All(result, frame =>
        {
            Assert.Equal(320u, frame.Width);
            Assert.Equal(240u, frame.Height);
        });
    }

    [Fact]
    public void NotAnImage_IsRejected()
    {
        var content = Encoding.UTF8.GetBytes("Hello, I am text");

        Assert.False(_processor.TryProcess(content, out _, out var error));
        Assert.Contains("not a JPG, GIF or PNG", error);
    }

    [Fact]
    public void SvgRenamedToPng_IsRejected()
    {
        var content = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

        Assert.False(_processor.TryProcess(content, out _, out _));
    }

    [Fact]
    public void BrokenPng_IsRejected()
    {
        // Real PNG signature, then garbage
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

        Assert.False(_processor.TryProcess(content, out _, out var error));
        Assert.Contains("damaged", error);
    }

    [Fact]
    public void ImageWithTooLongSide_IsRejectedBeforeDecoding()
    {
        var content = CreateImage(MagickFormat.Png, MagickImageProcessor.MaxSourceSide + 1, 10);

        Assert.False(_processor.TryProcess(content, out _, out var error));
        Assert.Contains("too large", error);
    }

    [Fact]
    public void ImageWithTooManyPixels_IsRejected()
    {
        // Each side is allowed, but 6000 x 4500 = 27 megapixels is over the 25 MP limit
        var content = CreateImage(MagickFormat.Png, 6000, 4500);

        Assert.False(_processor.TryProcess(content, out _, out var error));
        Assert.Contains("too large", error);
    }

    [Fact]
    public void AnimatedGif_AllFramesAreCountedForTheLimit()
    {
        // One frame is small (1 MP), but 30 of them need memory for 30 MP
        using var source = new MagickImageCollection();
        for (var i = 0; i < 30; i++)
        {
            source.Add(new MagickImage(MagickColors.Red, 1000, 1000));
        }
        var content = source.ToByteArray(MagickFormat.Gif);

        Assert.False(_processor.TryProcess(content, out _, out var error));
        Assert.Contains("too large", error);
    }
}
