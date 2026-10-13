namespace Comments.Application.Comments;

// Text is the HTML exactly as it would be saved: only the allowed tags, everything else encoded
public record CommentPreviewResponse(string Text);
