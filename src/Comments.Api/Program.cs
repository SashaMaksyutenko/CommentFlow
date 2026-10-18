using Comments.Api.ErrorHandling;
using Comments.Api.HealthChecks;
using Comments.Api.Hubs;
using Comments.Api.RateLimiting;
using Comments.Application.Attachments;
using Comments.Application.Comments;
using Comments.Application.Comments.Events;
using Comments.Application.Users;
using Comments.Infrastructure;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Relative path is resolved from the project folder, an absolute one (Docker volume) is used as is
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["FileStorage:RootPath"] ?? "uploads");

// Disk by default, Database on hosts without a lasting disk. A wrong value stops the API at start.
var fileStorage = builder.Configuration.GetValue("FileStorage:Provider", FileStorageProvider.Disk);

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("CommentsDb"),
    builder.Configuration.GetConnectionString("Redis"),
    builder.Configuration.GetConnectionString("RabbitMq"),
    uploadsPath,
    fileStorage);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<ICommentTextSanitizer, CommentTextSanitizer>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IAttachmentProcessingService, AttachmentProcessingService>();

// Finds all event handlers (INotificationHandler) in two projects:
// Application (cache) and this one (live updates through SignalR)
builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssemblies(typeof(CommentCreated).Assembly, typeof(Program).Assembly));

builder.Services.AddSignalR();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Default is 32, and every level of replies takes 2 (object + "replies" array),
    // so a thread deeper than ~15 replies failed with 500. Now about 250 levels fit.
    options.JsonSerializerOptions.MaxDepth = 512;
});

// Errors are returned in the standard "problem details" JSON format
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Only the visitor's address: nginx puts it into the X-Forwarded-For header
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;

    // By default only a proxy on this machine (127.0.0.1) is trusted. nginx is another container
    // with a changing address, so the header is trusted from any sender. That is safe only
    // because the API is reachable from outside through nginx alone (see docker-compose).
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// A frontend on another address (Vercel) may call the API and SignalR only from the addresses listed here.
// Empty by default: in Docker the browser gets the page and the API from one address and needs no CORS.
// A trailing "/" is cut off, because the browser sends the address without it.
var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Select(origin => origin.TrimEnd('/'))
    .ToArray();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    // The SignalR client sends its requests "with credentials", and then the browser wants this
    .AllowCredentials()
    // The browser may skip the extra OPTIONS request for 10 minutes
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

var app = builder.Build();

// In Docker the API brings the database up to date itself, before it takes any request.
// Locally this is off: the schema is updated with "dotnet ef database update".
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

// First in the pipeline, so it catches exceptions from everything below
app.UseExceptionHandler();

// Behind nginx every request comes from nginx's address. This puts the visitor's own address back,
// before the rate limiter and the controllers read it. Off when the API is reached directly:
// then anyone could write any address into the header and get around the limits.
if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    app.UseForwardedHeaders();
}

// Security headers for every response:
// nosniff - the browser must not guess the content type (a .txt must never run as HTML),
// DENY - the API can't be shown inside a frame on another site (clickjacking).
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    await next();
});

// Before the rate limiter: the browser's OPTIONS check is answered here and doesn't use up the limits
app.UseCors();

app.UseRateLimiter();

// Liveness: the API process is up. Runs no checks.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

// Readiness: the API can reach its dependencies (database).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Controllers include UploadsController, which gives out the uploaded files at /uploads/<name>
app.MapControllers();
app.MapHub<CommentsHub>(CommentsHub.Path);

app.Run();
