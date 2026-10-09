using System.Security.Cryptography;
using Comments.Application.Captcha;
using Microsoft.Extensions.Caching.Distributed;

namespace Comments.Infrastructure.Captcha;

public class CaptchaService : ICaptchaService
{
    public const int CodeLength = 6;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    // No 0/O, 1/I/L: they look too similar on the image
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly IDistributedCache _cache;
    private readonly ICaptchaImageGenerator _imageGenerator;

    public CaptchaService(IDistributedCache cache, ICaptchaImageGenerator imageGenerator)
    {
        _cache = cache;
        _imageGenerator = imageGenerator;
    }

    public async Task<CaptchaChallenge> CreateAsync(CancellationToken cancellationToken)
    {
        // Crypto random, so the next code can't be predicted
        var code = RandomNumberGenerator.GetString(Alphabet, CodeLength);
        var id = Guid.NewGuid().ToString("N");

        await _cache.SetStringAsync(CacheKey(id), code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime
        }, cancellationToken);

        return new CaptchaChallenge(id, _imageGenerator.GeneratePng(code));
    }

    public async Task<bool> ValidateAsync(string captchaId, string answer, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(answer))
        {
            return false;
        }

        var key = CacheKey(captchaId);
        var code = await _cache.GetStringAsync(key, cancellationToken);
        if (code is null)
        {
            return false;
        }

        // Remove even on a wrong answer, so nobody can try many answers for one image
        await _cache.RemoveAsync(key, cancellationToken);

        return string.Equals(code, answer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string CacheKey(string captchaId) => $"captcha:{captchaId}";
}
