namespace Comments.Application.Captcha;

// The answer is not here on purpose: it stays on the server
public record CaptchaChallenge(string Id, byte[] PngImage);
