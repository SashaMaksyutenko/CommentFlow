using System.Text.Json;
using Comments.Application.Comments;
using Comments.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    // [ApiController] returns 400 by itself when the request attributes fail,
    // so this method only runs for a valid request
    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            var comment = await _commentService.CreateAsync(request, GetClientIp(), userAgent, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, comment);
        }
        catch (FieldValidationException ex)
        {
            // Same 400 format as the automatic validation, with a camelCase field name
            ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(ex.Field), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    private string? GetClientIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress;
        if (ip is null)
        {
            return null;
        }

        // "::ffff:127.0.0.1" -> "127.0.0.1"
        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString();
    }
}
