using Comments.Application.Attachments;
using Comments.Application.Captcha;
using Comments.Application.Comments;
using Comments.Application.Users;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Images;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString, string uploadsPath)
    {
        // Fail fast: without a connection string the API cannot work at all.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CommentsDb' is not configured.");
        }

        services.AddDbContext<CommentsDbContext>(options =>
            // Retries transient failures, e.g. while SQL Server is still starting up.
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();

        // In-memory for now, will be replaced with Redis
        services.AddDistributedMemoryCache();

        services.AddSingleton<ICaptchaImageGenerator, CaptchaImageGenerator>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        services.AddSingleton<IImageProcessor, MagickImageProcessor>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(uploadsPath));

        return services;
    }
}
