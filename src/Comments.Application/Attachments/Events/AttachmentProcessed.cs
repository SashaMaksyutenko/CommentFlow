using MediatR;

namespace Comments.Application.Attachments.Events;

// Published when the processing of an uploaded file is finished, well or not.
// Attachment has the final status (Ready or Failed) and the final size.
public record AttachmentProcessed(int CommentId, AttachmentResponse Attachment) : INotification;
