using Comments.Application.Comments;

namespace Comments.Api.Models;

// What the browser sends: the comment fields plus an optional file.
// IFormFile is an ASP.NET type, so it lives here and not in the Application layer.
public class CreateCommentForm : CreateCommentRequest
{
    public IFormFile? File { get; set; }
}
