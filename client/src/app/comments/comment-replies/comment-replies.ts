import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommentModel } from '../../models/comment.model';
import { CommentsApiService } from '../../services/comments-api.service';
import { ReplyList } from '../reply-list/reply-list';

// Loads the replies of one comment and shows them as a tree
@Component({
  imports: [ReplyList],
  selector: 'app-comment-replies',
  styleUrl: './comment-replies.css',
  templateUrl: './comment-replies.html',
})
export class CommentReplies implements OnInit, OnDestroy {
  private readonly api = inject(CommentsApiService);
  private request?: Subscription;

  readonly commentId = input.required<number>();

  // null until the replies are loaded
  protected readonly replies = signal<CommentModel[] | null>(null);
  protected readonly loadFailed = signal(false);

  // Not in the constructor: the input has no value there yet
  ngOnInit(): void {
    this.request = this.api.getReplies(this.commentId()).subscribe({
      next: (replies) => this.replies.set(replies),
      error: () => this.loadFailed.set(true),
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }
}
