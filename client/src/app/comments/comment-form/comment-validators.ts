import { AbstractControl, ValidationErrors } from '@angular/forms';

// The rules here repeat the server ones (CreateCommentRequest), so the visitor
// sees a mistake before sending. The server still checks everything again.

export const USER_NAME_PATTERN = /^[A-Za-z0-9]+$/;

// The domain needs a dot and an ending of 2 or more letters
export const EMAIL_PATTERN = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

export const MAX_LENGTH = { userName: 50, email: 254, homePage: 2048, text: 5000 };

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
