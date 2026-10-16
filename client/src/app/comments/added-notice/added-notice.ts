import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommentEventsService } from '../../services/comment-events.service';

// How long the notice stays on the screen
export const NOTICE_TIME_MS = 4000;

// A short line at the bottom of the screen after the visitor's own comment is added.
// The visitor sees it wherever the page is scrolled to, even if the new comment itself
// is not on the screen (another page of the table, another order).
@Component({
  selector: 'app-added-notice',
  styleUrl: './added-notice.css',
  templateUrl: './added-notice.html',
})
export class AddedNotice implements OnInit, OnDestroy {
  private readonly events = inject(CommentEventsService);
  private subscription?: Subscription;
  private timer?: ReturnType<typeof setTimeout>;

  // The text on the screen now, empty when there is nothing to tell
  protected readonly message = signal('');

  ngOnInit(): void {
    // Only the visitor's own comments: comments of other people come in quietly
    this.subscription = this.events.created$.subscribe(({ comment, own }) => {
      if (own) {
        this.show(comment.parentId === null ? 'Your comment was added.' : 'Your reply was added.');
      }
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    clearTimeout(this.timer);
  }

  private show(text: string): void {
    this.message.set(text);

    // A second comment soon after the first one starts the time again
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.message.set(''), NOTICE_TIME_MS);
  }
}
