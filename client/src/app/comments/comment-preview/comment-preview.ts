import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription } from 'rxjs';
import { CommentsApiService } from '../../services/comments-api.service';
import { fieldErrorsOf } from '../comment-form/server-errors';

// The Preview button and the comment text the way it will look after saving.
// The server makes the preview, so it follows exactly the same rules as a real comment.
@Component({
  selector: 'app-comment-preview',
  styleUrl: './comment-preview.css',
  templateUrl: './comment-preview.html',
})
export class CommentPreview implements OnInit, OnDestroy {
  private readonly api = inject(CommentsApiService);
  private request?: Subscription;
  private textChanges?: Subscription;

  // The control of the comment text in the form
  readonly control = input.required<FormControl<string>>();

  // The text as the server would save it, null while there is nothing to show
  protected readonly html = signal<string | null>(null);
  protected readonly loading = signal(false);

  // Why there is no preview when the text itself is not the reason (the limit, the server is down)
  protected readonly failure = signal('');

  ngOnInit(): void {
    // A preview of an old text would mislead. As soon as the text changes,
    // the preview goes away and an answer that is still on its way is dropped.
    this.textChanges = this.control().valueChanges.subscribe(() => {
      this.request?.unsubscribe();
      this.loading.set(false);
      this.html.set(null);
      this.failure.set('');
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.textChanges?.unsubscribe();
  }

  protected show(): void {
    const control = this.control();
    if (control.invalid) {
      // Empty, too long or already refused: the form shows the reason under the field
      control.markAsTouched();
      return;
    }

    this.loading.set(true);
    this.failure.set('');

    this.request?.unsubscribe();
    this.request = this.api.previewComment(control.value).subscribe({
      next: (html) => {
        this.loading.set(false);
        this.html.set(html);
      },
      error: (response: HttpErrorResponse) => {
        this.loading.set(false);
        this.html.set(null);
        this.showFailure(response);
      },
    });
  }

  private showFailure(response: HttpErrorResponse): void {
    const textErrors = fieldErrorsOf(response)['Text'] ?? [];

    if (textErrors.length > 0) {
      // A bad tag: the message goes under the text field, where the form shows its own errors
      this.control().setErrors({ server: textErrors[0] });
      this.control().markAsTouched();
    } else if (response.status === 429) {
      this.failure.set('Too many previews in a short time. Wait a minute and try again.');
    } else {
      this.failure.set('Could not make the preview. Try again later.');
    }
  }
}
