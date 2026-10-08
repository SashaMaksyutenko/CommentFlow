using Comments.Api.HealthChecks;
using Comments.Application.Users;
using Comments.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("CommentsDb"));

builder.Services.AddScoped<IUserService, UserService>();

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

app.Run();
