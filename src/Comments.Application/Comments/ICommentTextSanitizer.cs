namespace Comments.Application.Comments;

public interface ICommentTextSanitizer
{
    // Returns false and an error message if the text has a forbidden or unclosed tag
    bool TrySanitize(string input, out string html, out string error);
}
