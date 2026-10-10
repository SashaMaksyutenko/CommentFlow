using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Comments.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public const string CaptchaPolicy = "captcha";
    public const string CommentsPolicy = "comments";

    // Limits are per client IP and per minute. Reading comments is not limited.
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var captchaPerMinute = configuration.GetValue("RateLimits:CaptchaPerMinute", 30);
        var commentsPerMinute = configuration.GetValue("RateLimits:CommentsPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(CaptchaPolicy, context => PerClientPerMinute(context, captchaPerMinute));
            options.AddPolicy(CommentsPolicy, context => PerClientPerMinute(context, commentsPerMinute));
        });

        return services;
    }

    // Every IP gets its own counter that resets each minute
    private static RateLimitPartition<string> PerClientPerMinute(HttpContext context, int permitLimit)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1)
        });
    }
}
