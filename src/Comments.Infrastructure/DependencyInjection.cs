using Comments.Application.Attachments;
using Comments.Application.Captcha;
using Comments.Application.Comments;
using Comments.Application.Users;
using Comments.Infrastructure.Caching;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Images;
using Comments.Infrastructure.Messaging;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString,
        string? redisConnectionString,
        string? rabbitMqConnectionString,
        string uploadsPath)
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
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();

        // Both register the same IDistributedCache interface, so the code that uses the cache
        // (captcha) doesn't know which one it got.
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // No Redis configured: keep the cache in this process (enough for tests and one instance)
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                // Prefix for all our keys in Redis, e.g. "comments:captcha:<id>"
                options.InstanceName = "comments:";
            });
        }

        services.AddSingleton<ICommentCache, DistributedCommentCache>();

        services.AddSingleton<ICaptchaImageGenerator, CaptchaImageGenerator>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        services.AddSingleton<IImageProcessor, MagickImageProcessor>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(uploadsPath));

        AddMessaging(services, rabbitMqConnectionString);

        return services;
    }

    // MassTransit hides the queue behind its own API, so the consumer is the same
    // for RabbitMQ and for the in-memory queue
    private static void AddMessaging(IServiceCollection services, string? rabbitMqConnectionString)
    {
        services.AddMassTransit(config =>
        {
            config.AddConsumer<ProcessAttachmentConsumer>();

            // Queue names like "process-attachment" instead of "ProcessAttachment"
            config.SetKebabCaseEndpointNameFormatter();

            if (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
            {
                // No RabbitMQ configured: the queue lives inside this process (enough for tests)
                config.UsingInMemory((context, bus) =>
                {
                    bus.UseMessageRetry(RetryFailedMessages);
                    bus.ConfigureEndpoints(context);
                });
            }
            else
            {
                config.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(new Uri(rabbitMqConnectionString));
                    bus.UseMessageRetry(RetryFailedMessages);

                    // Creates a queue for every registered consumer
                    bus.ConfigureEndpoints(context);
                });
            }
        });
    }

    // If a consumer throws (e.g. the database is down for a moment), the message is tried
    // 3 more times with 5 seconds between. After that it goes to a separate "_error" queue.
    private static void RetryFailedMessages(IRetryConfigurator retry)
    {
        retry.Interval(3, TimeSpan.FromSeconds(5));
    }
}
