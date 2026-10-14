import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommentModel } from '../../models/comment.model';
import { CommentEventsService } from '../../services/comment-events.service';
import { CommentsApiService } from '../../services/comments-api.service';
import { ReplyList } from '../reply-list/reply-list';
import { replaceAttachment } from '../replace-attachment';

// Loads the replies of one comment and shows them as a tree
@Component({
  imports: [ReplyList],
  selector: 'app-comment-replies',
  styleUrl: './comment-replies.css',
  templateUrl: './comment-replies.html',
})
export class CommentReplies implements OnInit, OnDestroy {
  private readonly api = inject(CommentsApiService);
  private readonly events = inject(CommentEventsService);
  private request?: Subscription;

  // Listening for new replies and for processed images
  private createdSubscription?: Subscription;
  private processedSubscription?: Subscription;

  readonly commentId = input.required<number>();

  // null until the replies are loaded
  protected readonly replies = signal<CommentModel[] | null>(null);
  protected readonly loadFailed = signal(false);

  // Not in the constructor: the input has no value there yet
  ngOnInit(): void {
    this.load();

    // A new reply (the visitor's own or someone else's) may belong to this thread, so the thread
    // is loaded again. The replies that are already shown stay on the screen until the new ones arrive.
    this.createdSubscription = this.events.created$.subscribe(({ comment }) => {
      if (comment.parentId !== null) {
        this.load();
      }
    });

    // An image of a reply in this thread is ready: only its attachment changes, nothing is loaded
    this.processedSubscription = this.events.attachmentProcessed$.subscribe(({ commentId, attachment }) => {
      this.replies.update((replies) => replies && replaceAttachment(replies, commentId, attachment));
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.createdSubscription?.unsubscribe();
    this.processedSubscription?.unsubscribe();
  }

  private load(): void {
    this.loadFailed.set(false);

    this.request?.unsubscribe();
    this.request = this.api.getReplies(this.commentId()).subscribe({
      next: (replies) => this.replies.set(replies),
      error: () => this.loadFailed.set(true),
    });
  }
}
