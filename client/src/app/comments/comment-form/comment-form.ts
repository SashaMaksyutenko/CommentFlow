import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommentEventsService } from '../../services/comment-events.service';
import { CommentsApiService } from '../../services/comments-api.service';
import { ReplyTargetService } from '../../services/reply-target.service';
import { CaptchaField } from '../captcha-field/captcha-field';
import { CommentPreview } from '../comment-preview/comment-preview';
import { TagToolbar } from '../tag-toolbar/tag-toolbar';
import { AttachmentField } from '../attachment-field/attachment-field';
import { EMAIL_PATTERN, MAX_LENGTH, USER_NAME_PATTERN, allowedFile, httpUrl, notBlank } from './comment-validators';
import { fieldErrorsOf } from './server-errors';

type CommentFormField = 'userName' | 'email' | 'homePage' | 'text' | 'file' | 'captchaAnswer';

// The fields that have a place for a message under them
const FORM_FIELDS: CommentFormField[] = ['userName', 'email', 'homePage', 'text', 'file', 'captchaAnswer'];

@Component({
  imports: [ReactiveFormsModule, AttachmentField, CaptchaField, CommentPreview, TagToolbar],
  selector: 'app-comment-form',
  styleUrl: './comment-form.css',
  templateUrl: './comment-form.html',
})
export class CommentForm {
  private readonly api = inject(CommentsApiService);
  private readonly events = inject(CommentEventsService);

  // Which comment is being answered, if any. The template shows it above the fields.
  protected readonly replyTarget = inject(ReplyTargetService);

  // "Non nullable" means an empty field is '' and never null
  private readonly formBuilder = inject(NonNullableFormBuilder);

  // The CAPTCHA field from the template, to ask it for a new picture
  private readonly captchaField = viewChild.required(CaptchaField);

  // The <textarea> from the template, to put the cursor into it
  private readonly textField = viewChild.required<ElementRef<HTMLTextAreaElement>>('textField');

  // What happened with the last sending: nothing yet, in progress, done or refused
  protected readonly status = signal<'idle' | 'sending' | 'sent' | 'failed'>('idle');

  // Why the last sending was refused, shown above the button
  protected readonly failure = signal('');

  // Every field: its starting value and the rules it must pass
  protected readonly form = this.formBuilder.group({
    userName: ['', [notBlank, Validators.maxLength(MAX_LENGTH.userName), Validators.pattern(USER_NAME_PATTERN)]],
    email: ['', [notBlank, Validators.maxLength(MAX_LENGTH.email), Validators.pattern(EMAIL_PATTERN)]],
    homePage: ['', [Validators.maxLength(MAX_LENGTH.homePage), httpUrl]],
    text: ['', [notBlank, Validators.maxLength(MAX_LENGTH.text)]],
    // The chosen file, null while nothing is attached
    file: [null as File | null, allowedFile],
    // Filled in by the CAPTCHA field when a picture is loaded. No picture, no sending.
    captchaId: ['', Validators.required],
    captchaAnswer: ['', [notBlank, Validators.maxLength(MAX_LENGTH.captchaAnswer)]],
  });

  constructor() {
    // Runs every time a Reply button is pressed somewhere on the page: the cursor goes to the
    // text field. The browser scrolls to a field that gets the cursor, so the form comes into view.
    effect(() => {
      if (this.replyTarget.target() !== null) {
        this.textField().nativeElement.focus();
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      // Show the errors of the fields the visitor has not been in yet
      this.form.markAllAsTouched();
      return;
    }

    // null when this is a new top-level comment and not a reply
    const parentId = this.replyTarget.target()?.id ?? null;

    this.status.set('sending');
    this.api.createComment({ ...this.form.getRawValue(), parentId }).subscribe({
      next: (comment) => {
        this.status.set('sent');
        // The name, e-mail and home page stay for the next comment
        this.form.controls.text.reset();
        this.form.controls.file.reset();
        this.captchaField().reload();
        // The reply is sent, the next comment is an ordinary one again
        this.replyTarget.clear();
        // The rest of the page (the table) finds out about the new comment from here
        this.events.notifyOwnComment(comment);
      },
      error: (response: HttpErrorResponse) => {
        this.status.set('failed');

        // The picture is still good in two cases: the limit stopped the request before any check,
        // or the text is the only thing refused (the server checks the text before the CAPTCHA).
        // In every other case the picture is used up and a new one is needed.
        const refused = Object.keys(fieldErrorsOf(response));
        const onlyTextRefused = refused.length === 1 && refused[0].toLowerCase() === 'text';
        const captchaStillGood = response.status === 429 || onlyTextRefused;
        if (!captchaStillGood) {
          this.captchaField().reload();
        }

        // After the reload: a new picture clears the CAPTCHA field together with its error
        this.showFailure(response);
      },
    });
  }

  // Puts the reason of a refusal on the screen: under the fields and above the button
  private showFailure(response: HttpErrorResponse): void {
    if (response.status === 429) {
      this.failure.set('Too many comments in a short time. Wait a minute and try again.');
      return;
    }

    const errors = Object.entries(fieldErrorsOf(response));
    if (errors.length === 0) {
      this.failure.set('Could not send the comment. Try again later.');
      return;
    }

    // Messages about things that are not fields of this form
    const otherMessages: string[] = [];

    for (const [serverName, messages] of errors) {
      // "UserName" from the server is "userName" here
      const field = FORM_FIELDS.find((name) => name.toLowerCase() === serverName.toLowerCase());
      if (field) {
        this.form.controls[field].setErrors({ server: messages[0] });
        this.form.controls[field].markAsTouched();
      } else {
        otherMessages.push(...messages);
      }
    }

    this.failure.set(
      otherMessages.length > 0 ? otherMessages.join(' ') : 'The comment was not added. Check the fields marked in red.',
    );
  }

  // The message under a field. Empty while the field is fine or the visitor has not left it yet.
  protected errorOf(field: CommentFormField): string {
    const control = this.form.controls[field];
    if (control.valid || !control.touched) {
      return '';
    }

    // What the server said about this field. It goes away when the visitor changes the field.
    const serverMessage: unknown = control.getError('server');
    if (typeof serverMessage === 'string') {
      return serverMessage;
    }

    if (control.hasError('required')) {
      return 'This field is required.';
    }
    if (field !== 'file' && control.hasError('maxlength')) {
      return `No more than ${MAX_LENGTH[field]} characters.`;
    }
    if (control.hasError('pattern')) {
      return field === 'userName' ? 'Only Latin letters and digits are allowed.' : 'E-mail format is invalid.';
    }
    if (control.hasError('httpUrl')) {
      return 'The address must start with http:// or https://.';
    }
    if (control.hasError('fileType')) {
      return 'Only JPG, GIF, PNG images and TXT files are allowed.';
    }
    if (control.hasError('imageTooBig')) {
      return 'An image can be up to 5 MB.';
    }
    if (control.hasError('textFileTooBig')) {
      return 'A TXT file can be up to 100 KB.';
    }

    return '';
  }
}
