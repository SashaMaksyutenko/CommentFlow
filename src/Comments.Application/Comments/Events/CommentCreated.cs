using MediatR;

namespace Comments.Application.Comments.Events;

// Published after a comment is saved. Anyone who needs to react adds a handler,
// the comment service itself doesn't know about them.
public record CommentCreated(CommentResponse Comment) : INotification;
