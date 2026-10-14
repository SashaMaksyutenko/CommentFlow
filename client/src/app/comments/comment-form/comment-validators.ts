import { AbstractControl, ValidationErrors } from '@angular/forms';

// The rules here repeat the server ones (CreateCommentRequest), so the visitor
// sees a mistake before sending. The server still checks everything again.

export const USER_NAME_PATTERN = /^[A-Za-z0-9]+$/;

// The domain needs a dot and an ending of 2 or more letters
export const EMAIL_PATTERN = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

export const MAX_LENGTH = { userName: 50, email: 254, homePage: 2048, text: 5000, captchaAnswer: 20 };

// File rules from the server (AttachmentService)
export const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif'];
export const MAX_IMAGE_SIZE = 5 * 1024 * 1024;
export const MAX_TEXT_FILE_SIZE = 100 * 1024;

// No file is fine, the field is optional. Otherwise a JPG, GIF or PNG image up to 5 MB
// or a TXT file up to 100 KB. Only the name and the size are checked here,
// the server also looks inside the file.
export function allowedFile(control: AbstractControl<File | null>): ValidationErrors | null {
  const file = control.value;
  if (file === null) {
    return null;
  }

  const name = file.name.toLowerCase();
  if (name.endsWith('.txt')) {
    return file.size > MAX_TEXT_FILE_SIZE ? { textFileTooBig: true } : null;
  }
  if (IMAGE_EXTENSIONS.some((extension) => name.endsWith(extension))) {
    return file.size > MAX_IMAGE_SIZE ? { imageTooBig: true } : null;
  }

  return { fileType: true };
}

// Like Validators.required, but a value of spaces only counts as empty too
export function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === '' ? { required: true } : null;
}

// Empty is fine because the field is optional, otherwise only http and https addresses pass
export function httpUrl(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  if (value === '') {
    return null;
  }

  if (!URL.canParse(value)) {
    return { httpUrl: true };
  }

  const protocol = new URL(value).protocol;
  return protocol === 'http:' || protocol === 'https:' ? null : { httpUrl: true };
}
