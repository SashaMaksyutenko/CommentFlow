using Comments.Api.HealthChecks;
using Comments.Application.Attachments;
using Comments.Application.Comments;
using Comments.Application.Users;
using Comments.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Relative path is resolved from the project folder, an absolute one (Docker volume) is used as is
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["FileStorage:RootPath"] ?? "uploads");

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("CommentsDb"), uploadsPath);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<ICommentTextSanitizer, CommentTextSanitizer>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

builder.Services.AddControllers();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

// Liveness: the API process is up. Runs no checks.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

// Readiness: the API can reach its dependencies (database).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
