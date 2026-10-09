using Comments.Infrastructure.Captcha;
using ImageMagick;

namespace Comments.Tests.Infrastructure;

public class CaptchaImageGeneratorTests
{
    [Fact]
    public void GeneratePng_ReturnsPngOfExpectedSize()
    {
        var generator = new CaptchaImageGenerator();

        var bytes = generator.GeneratePng("AB12CD");
        var info = new MagickImageInfo(bytes);

        Assert.Equal(MagickFormat.Png, info.Format);
        Assert.Equal((uint)CaptchaImageGenerator.Width, info.Width);
        Assert.Equal((uint)CaptchaImageGenerator.Height, info.Height);
    }
}
