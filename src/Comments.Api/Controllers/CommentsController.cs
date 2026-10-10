using Comments.Api.Models;
using Comments.Application.Attachments;
using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    // 5 MB image limit + room for the text fields
    private const int MaxRequestSize = 6 * 1024 * 1024;

    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    // Top-level comments only, 25 per page.
    // Example: GET /api/comments?page=2&sortBy=userName&sortDirection=asc
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CommentResponse>>> GetTopLevel(
        [FromQuery] GetCommentsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _commentService.GetTopLevelAsync(query, cancellationToken));
    }

    // All replies under the comment as a tree (each reply has its own "replies")
    [HttpGet("{id:int}/replies")]
    public async Task<ActionResult<List<CommentResponse>>> GetReplies(int id, CancellationToken cancellationToken)
    {
        var replies = await _commentService.GetRepliesAsync(id, cancellationToken);
        return replies is null ? NotFound() : Ok(replies);
    }

    // Sent as multipart/form-data: the form fields plus an optional "file".
    // [ApiController] returns 400 by itself when the request attributes fail,
    // so this method only runs for a valid request.
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestSize)]
    public async Task<ActionResult<CommentResponse>> Create([FromForm] CreateCommentForm form, CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var upload = form.File is null ? null : await ReadFileAsync(form.File, cancellationToken);

        try
        {
            var comment = await _commentService.CreateAsync(form, upload, GetClientIp(), userAgent, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, comment);
        }
        catch (FieldValidationException ex)
        {
            // Same 400 format and field names ("CaptchaAnswer") as the automatic validation
            ModelState.AddModelError(ex.Field, ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    // IFormFile is an ASP.NET type, the Application layer gets plain bytes
    private static async Task<UploadedFile> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        return new UploadedFile(file.FileName, memory.ToArray());
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
