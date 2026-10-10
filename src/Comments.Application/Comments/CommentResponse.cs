using Comments.Application.Attachments;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

// IP and User-Agent are not returned on purpose
public record CommentResponse(
    int Id,
    int? ParentId,
    string UserName,
    string Email,
    string? HomePage,
    string Text,
    DateTime CreatedAt)
{
    // null if the comment has no file
    public AttachmentResponse? Attachment { get; init; }

    // Filled only by the replies endpoint, empty in the top-level list
    public List<CommentResponse> Replies { get; init; } = [];

    public static CommentResponse From(Comment comment, User user)
    {
        return new CommentResponse(
            comment.Id,
            comment.ParentId,
            user.UserName,
            user.Email,
            user.HomePage,
            comment.Text,
            comment.CreatedAt)
        {
            Attachment = comment.Attachment is null ? null : AttachmentResponse.From(comment.Attachment)
        };
    }
}
