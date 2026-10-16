import { Injectable, inject, signal } from '@angular/core';
import { CommentModel } from '../models/comment.model';
import { ReplyTargetService } from './reply-target.service';

// Opens and closes the window with the comment form.
// "Add a comment" opens it for a new comment, a Reply button opens it for an answer.
@Injectable({ providedIn: 'root' })
export class CommentDialogService {
  private readonly replyTarget = inject(ReplyTargetService);
  private readonly isOpenSignal = signal(false);

  // For reading only: the window decides by it whether to show itself
  readonly isOpen = this.isOpenSignal.asReadonly();

  openNew(): void {
    // An answer that was started before and not sent should not stay in the form
    this.replyTarget.clear();
    this.isOpenSignal.set(true);
  }

  openReply(comment: CommentModel): void {
    this.replyTarget.replyTo(comment);
    this.isOpenSignal.set(true);
  }

  close(): void {
    this.isOpenSignal.set(false);
  }
}
