using Comments.Application.Common;

namespace Comments.Application.Comments;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(
        CreateCommentRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<PagedResponse<CommentResponse>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken cancellationToken);

    // Returns null if the comment doesn't exist
    Task<List<CommentResponse>?> GetRepliesAsync(int commentId, CancellationToken cancellationToken);
}
