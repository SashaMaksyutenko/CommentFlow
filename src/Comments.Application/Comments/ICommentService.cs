namespace Comments.Application.Comments;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(
        CreateCommentRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
