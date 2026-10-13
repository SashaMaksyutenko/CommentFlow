import { Component, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommentsApiService } from '../../services/comments-api.service';
import { CaptchaField } from '../captcha-field/captcha-field';
import { EMAIL_PATTERN, MAX_LENGTH, USER_NAME_PATTERN, httpUrl, notBlank } from './comment-validators';

type CommentFormField = 'userName' | 'email' | 'homePage' | 'text' | 'captchaAnswer';

@Component({
  imports: [ReactiveFormsModule, CaptchaField],
  selector: 'app-comment-form',
  styleUrl: './comment-form.css',
  templateUrl: './comment-form.html',
})
export class CommentForm {
  private readonly api = inject(CommentsApiService);

  // "Non nullable" means an empty field is '' and never null
  private readonly formBuilder = inject(NonNullableFormBuilder);

  // The CAPTCHA field from the template, to ask it for a new picture
  private readonly captchaField = viewChild.required(CaptchaField);

  // What happened with the last sending: nothing yet, in progress, done or refused
  protected readonly status = signal<'idle' | 'sending' | 'sent' | 'failed'>('idle');

  // Every field: its starting value and the rules it must pass
  protected readonly form = this.formBuilder.group({
    userName: ['', [notBlank, Validators.maxLength(MAX_LENGTH.userName), Validators.pattern(USER_NAME_PATTERN)]],
    email: ['', [notBlank, Validators.maxLength(MAX_LENGTH.email), Validators.pattern(EMAIL_PATTERN)]],
    homePage: ['', [Validators.maxLength(MAX_LENGTH.homePage), httpUrl]],
    text: ['', [notBlank, Validators.maxLength(MAX_LENGTH.text)]],
    // Filled in by the CAPTCHA field when a picture is loaded. No picture, no sending.
    captchaId: ['', Validators.required],
    captchaAnswer: ['', [notBlank, Validators.maxLength(MAX_LENGTH.captchaAnswer)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      // Show the errors of the fields the visitor has not been in yet
      this.form.markAllAsTouched();
      return;
    }

    this.status.set('sending');
    this.api.createComment(this.form.getRawValue()).subscribe({
      next: () => {
        this.status.set('sent');
        // The name, e-mail and home page stay for the next comment
        this.form.controls.text.reset();
        this.captchaField().reload();
      },
      error: () => {
        this.status.set('failed');
        // A CAPTCHA works only once, so the next try needs a new picture
        this.captchaField().reload();
      },
    });
  }

  // The message under a field. Empty while the field is fine or the visitor has not left it yet.
  protected errorOf(field: CommentFormField): string {
    const control = this.form.controls[field];
    if (control.valid || !control.touched) {
      return '';
    }

    if (control.hasError('required')) {
      return 'This field is required.';
    }
    if (control.hasError('maxlength')) {
      return `No more than ${MAX_LENGTH[field]} characters.`;
    }
    if (control.hasError('pattern')) {
      return field === 'userName' ? 'Only Latin letters and digits are allowed.' : 'E-mail format is invalid.';
    }
    if (control.hasError('httpUrl')) {
      return 'The address must start with http:// or https://.';
    }

    return '';
  }
}
