namespace Comments.Application.Attachments.Messages;

// Message for the queue: "process the uploaded file of this attachment".
// Only the id travels through the queue, the file itself stays in the storage.
public record ProcessAttachment(int AttachmentId);
