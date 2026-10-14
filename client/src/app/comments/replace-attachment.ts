import { AttachmentModel, CommentModel } from '../models/comment.model';

// A copy of the comments where the comment with this id has the new attachment.
// Works for the flat list of the table and for a tree of replies at any depth:
// for every comment it also looks through its replies.
// Copies and not changes, so the signals that hold the comments see a new value.
export function replaceAttachment(
  comments: CommentModel[],
  commentId: number,
  attachment: AttachmentModel,
): CommentModel[] {
  return comments.map((comment) =>
    comment.id === commentId
      ? { ...comment, attachment }
      : { ...comment, replies: replaceAttachment(comment.replies, commentId, attachment) },
  );
}
