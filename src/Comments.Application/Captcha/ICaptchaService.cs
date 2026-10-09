namespace Comments.Application.Captcha;

public interface ICaptchaService
{
    Task<CaptchaChallenge> CreateAsync(CancellationToken cancellationToken);

    // A captcha can be checked only once, after that it is removed
    Task<bool> ValidateAsync(string captchaId, string answer, CancellationToken cancellationToken);
}
