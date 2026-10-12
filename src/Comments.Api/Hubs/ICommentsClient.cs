using Comments.Application.Attachments;
using Comments.Application.Comments;

namespace Comments.Api.Hubs;

// What the server can send to the browsers. The method names are the names
// the client listens to: connection.on("CommentCreated", ...).
public interface ICommentsClient
{
    Task CommentCreated(CommentResponse comment);

    Task AttachmentProcessed(int commentId, AttachmentResponse attachment);
}
