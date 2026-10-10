using Comments.Api.ErrorHandling;
using Comments.Api.HealthChecks;
using Comments.Api.RateLimiting;
using Comments.Application.Attachments;
using Comments.Application.Comments;
using Comments.Application.Users;
using Comments.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Relative path is resolved from the project folder, an absolute one (Docker volume) is used as is
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["FileStorage:RootPath"] ?? "uploads");

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("CommentsDb"),
    builder.Configuration.GetConnectionString("Redis"),
    uploadsPath);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<ICommentTextSanitizer, CommentTextSanitizer>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

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

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

// First in the pipeline, so it catches exceptions from everything below
app.UseExceptionHandler();

// Security headers for every response:
// nosniff - the browser must not guess the content type (a .txt must never run as HTML),
// DENY - the API can't be shown inside a frame on another site (clickjacking).
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    await next();
});

app.UseRateLimiter();

// Liveness: the API process is up. Runs no checks.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

// Readiness: the API can reach its dependencies (database).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Uploaded files are served from /uploads/<generated name>.
// Only our four file types get a content type, anything else in the folder returns 404.
Directory.CreateDirectory(uploadsPath);
var uploadContentTypes = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".jpg"] = "image/jpeg",
    [".png"] = "image/png",
    [".gif"] = "image/gif",
    [".txt"] = "text/plain"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = AttachmentResponse.UrlPrefix,
    ContentTypeProvider = uploadContentTypes,
    OnPrepareResponse = context =>
    {
        // Text files are saved as UTF-8, say so or Cyrillic may look broken
        var response = context.Context.Response;
        if (response.ContentType == "text/plain")
        {
            response.ContentType = "text/plain; charset=utf-8";
        }
    }
});

app.MapControllers();

app.Run();
