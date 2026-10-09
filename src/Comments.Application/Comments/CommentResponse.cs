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
    public static CommentResponse From(Comment comment, User user)
    {
        return new CommentResponse(
            comment.Id,
            comment.ParentId,
            user.UserName,
            user.Email,
            user.HomePage,
            comment.Text,
            comment.CreatedAt);
    }
}
