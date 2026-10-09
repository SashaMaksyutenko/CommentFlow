namespace Comments.Infrastructure.Captcha;

public interface ICaptchaImageGenerator
{
    byte[] GeneratePng(string code);
}
