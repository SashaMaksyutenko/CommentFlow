// What the comment form sends to POST /api/comments
export interface NewComment {
  userName: string;
  email: string;

  // Empty when the visitor has no home page
  homePage: string;
  text: string;
  captchaId: string;
  captchaAnswer: string;
}
