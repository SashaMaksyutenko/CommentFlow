using ImageMagick;
using ImageMagick.Drawing;

namespace Comments.Infrastructure.Captcha;

public class CaptchaImageGenerator : ICaptchaImageGenerator
{
    public const int Width = 180;
    public const int Height = 60;

    // The font file is copied next to the dll, Linux containers have no system fonts
    private static readonly string FontPath =
        Path.Combine(AppContext.BaseDirectory, "Captcha", "Fonts", "ShareTechMono-Regular.ttf");

    public byte[] GeneratePng(string code)
    {
        using var image = new MagickImage(MagickColors.White, Width, Height);
        var drawables = new Drawables();

        // Noise lines make it harder to read the text automatically
        for (var i = 0; i < 6; i++)
        {
            drawables
                .StrokeColor(RandomColor())
                .StrokeWidth(1.5)
                .Line(Random.Shared.Next(Width), Random.Shared.Next(Height),
                      Random.Shared.Next(Width), Random.Shared.Next(Height));
        }

        drawables
            .StrokeColor(MagickColors.Transparent)
            .Font(FontPath)
            .FontPointSize(36);

        var charWidth = (double)Width / (code.Length + 1);

        // Each character gets its own position, angle and color
        for (var i = 0; i < code.Length; i++)
        {
            var x = charWidth * (i + 0.5);
            var y = 42 + Random.Shared.Next(-5, 6);
            var angle = Random.Shared.Next(-20, 21);

            drawables
                .PushGraphicContext()
                .Translation(x, y)
                .Rotation(angle)
                .FillColor(RandomColor())
                .Text(0, 0, code[i].ToString())
                .PopGraphicContext();
        }

        drawables.Draw(image);

        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    // Dark colors only, so the text stays readable on white
    private static MagickColor RandomColor()
    {
        return MagickColor.FromRgb(
            (byte)Random.Shared.Next(0, 140),
            (byte)Random.Shared.Next(0, 140),
            (byte)Random.Shared.Next(0, 140));
    }
}
