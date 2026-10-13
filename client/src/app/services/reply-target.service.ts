import { Injectable, signal } from '@angular/core';
import { CommentModel } from '../models/comment.model';

// Remembers which comment the visitor is answering.
// A Reply button anywhere on the page sets it, the comment form reads it.
@Injectable({ providedIn: 'root' })
export class ReplyTargetService {
  // "equal: () => false" makes every replyTo() count, even a second click on the same comment
  private readonly targetSignal = signal<CommentModel | null>(null, { equal: () => false });

  // null means a new top-level comment
  readonly target = this.targetSignal.asReadonly();

  replyTo(comment: CommentModel): void {
    this.targetSignal.set(comment);
  }

  clear(): void {
    this.targetSignal.set(null);
  }
}
