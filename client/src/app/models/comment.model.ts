// Shapes of the JSON the API returns.
// The names end with "Model" because "Comment" is already a browser type (an HTML comment node).

export interface AttachmentModel {
  url: string;
  fileName: string;
  type: 'Image' | 'Text';
  // The file can be opened only when the status is 'Ready'
  status: 'Processing' | 'Ready' | 'Failed';
  contentType: string;
  size: number;
}

export interface CommentModel {
  id: number;
  // null for a top-level comment
  parentId: number | null;
  userName: string;
  email: string;
  homePage: string | null;
  // HTML with the allowed tags only, already sanitized by the server
  text: string;
  // ISO date in UTC, e.g. "2026-10-01T10:27:00Z"
  createdAt: string;
  attachment: AttachmentModel | null;
  replies: CommentModel[];
}
