// What the comment form sends to POST /api/comments
export interface NewComment {
  userName: string;
  email: string;

  // Empty when the visitor has no home page
  homePage: string;
  text: string;

  // The comment that is answered, null for a top-level comment
  parentId: number | null;

  // An image or a TXT file, null when nothing is attached
  file: File | null;
  captchaId: string;
  captchaAnswer: string;
}
