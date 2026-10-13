// What GET /api/captcha returns. The answer itself never leaves the server.
export interface CaptchaModel {
  // Sent back together with the answer, so the server knows which picture was shown
  id: string;

  // The picture as a data URL, ready for <img src>
  image: string;
}
