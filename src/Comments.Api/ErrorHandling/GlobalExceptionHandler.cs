using Comments.Application.Validation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.ErrorHandling;

// One place that turns exceptions into HTTP responses, so controllers don't need try/catch
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = CreateProblem(exception);
        httpContext.Response.StatusCode = problem.Status!.Value;

        if (problem.Status >= 500)
        {
            // Full details go to the log only, never to the client
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static ProblemDetails CreateProblem(Exception exception)
    {
        // Wrong captcha, bad file etc: the same 400 format as the automatic validation
        if (exception is FieldValidationException validation)
        {
            var errors = new Dictionary<string, string[]> { [validation.Field] = [validation.Message] };
            return new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest };
        }

        // Anything else is a bug or a broken dependency, the client gets no details
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred. Please try again later."
        };
    }
}
