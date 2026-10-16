import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommentModel } from '../../models/comment.model';
import { CommentSortField, CommentsQuery, PagedResponse, SortDirection } from '../../models/paging.model';
import { CommentEventsService } from '../../services/comment-events.service';
import { CommentsApiService } from '../../services/comments-api.service';
import { CommentDialogService } from '../../services/comment-dialog.service';
import { AttachmentView } from '../attachment-view/attachment-view';
import { CommentReplies } from '../comment-replies/comment-replies';
import { replaceAttachment } from '../replace-attachment';
import { UserAvatar } from '../user-avatar/user-avatar';

@Component({
  imports: [DatePipe, AttachmentView, CommentReplies, UserAvatar],
  selector: 'app-comments-table',
  styleUrl: './comments-table.css',
  templateUrl: './comments-table.html',
})
export class CommentsTable implements OnInit, OnDestroy {
  private readonly api = inject(CommentsApiService);
  private readonly events = inject(CommentEventsService);

  // The Reply button of a row opens the comment form for an answer to this comment
  protected readonly dialog = inject(CommentDialogService);

  // The request that is running now, if any
  private request?: Subscription;

  // Listening for new comments and for processed images
  private createdSubscription?: Subscription;
  private processedSubscription?: Subscription;

  // Newest first by default (LIFO)
  protected readonly query = signal<CommentsQuery>({ page: 1, sortBy: 'createdAt', sortDirection: 'desc' });

  // null until the first page is loaded
  protected readonly page = signal<PagedResponse<CommentModel> | null>(null);
  protected readonly loading = signal(false);
  protected readonly loadFailed = signal(false);

  // Ids of the comments whose replies are shown now
  protected readonly openReplies = signal<number[]>([]);

  // The columns the table can be sorted by, in the order they are shown
  protected readonly sortableColumns: { field: CommentSortField; label: string }[] = [
    { field: 'userName', label: 'User Name' },
    { field: 'email', label: 'E-mail' },
    { field: 'createdAt', label: 'Date' },
  ];

  ngOnInit(): void {
    this.load();

    // A new top-level comment changes the list, so the page that is shown is loaded again.
    // A reply does not: the table has only top-level comments.
    this.createdSubscription = this.events.created$.subscribe(({ comment, own }) => {
      if (comment.parentId === null) {
        this.load();
      } else if (own) {
        // Only for the visitor's own reply: replies of other people must not open rows by themselves
        this.showRepliesOf(comment.parentId);
      }
    });

    // An image of a comment on this page is ready: only its attachment changes, nothing is loaded
    this.processedSubscription = this.events.attachmentProcessed$.subscribe(({ commentId, attachment }) => {
      this.page.update((page) => page && { ...page, items: replaceAttachment(page.items, commentId, attachment) });
    });
  }

  // After the visitor answers a comment of this page, its replies open, so the answer is seen at once
  private showRepliesOf(commentId: number): void {
    const comments = this.page()?.items ?? [];
    const isOnThisPage = comments.some((comment) => comment.id === commentId);

    if (isOnThisPage && !this.repliesOpen(commentId)) {
      this.openReplies.update((ids) => [...ids, commentId]);
    }
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.createdSubscription?.unsubscribe();
    this.processedSubscription?.unsubscribe();
  }

  protected sortBy(field: CommentSortField): void {
    const current = this.query();
    let sortDirection: SortDirection;

    if (current.sortBy === field) {
      // The same column again: turn the order around
      sortDirection = current.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      // Another column: dates start from the newest, names and e-mails from A
      sortDirection = field === 'createdAt' ? 'desc' : 'asc';
    }

    // With a new order the old page number means nothing, start from the first page
    this.query.set({ page: 1, sortBy: field, sortDirection });
    this.load();
  }

  protected goToPage(page: number): void {
    this.query.update((query) => ({ ...query, page }));
    this.load();
  }

  // For screen readers: which column is sorted and how
  protected ariaSort(field: CommentSortField): 'ascending' | 'descending' | null {
    const query = this.query();
    if (query.sortBy !== field) {
      return null;
    }

    return query.sortDirection === 'asc' ? 'ascending' : 'descending';
  }

  protected sortArrow(field: CommentSortField): string {
    const query = this.query();
    if (query.sortBy !== field) {
      return '';
    }

    return query.sortDirection === 'asc' ? '▲' : '▼';
  }

  protected repliesOpen(commentId: number): boolean {
    return this.openReplies().includes(commentId);
  }

  // Shows the replies of the comment, or hides them if they are shown
  protected toggleReplies(commentId: number): void {
    if (this.repliesOpen(commentId)) {
      this.openReplies.update((ids) => ids.filter((id) => id !== commentId));
    } else {
      this.openReplies.update((ids) => [...ids, commentId]);
    }
  }

  private load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);

    // Only the latest request matters, drop the previous one if it is still running
    this.request?.unsubscribe();
    this.request = this.api.getComments(this.query()).subscribe({
      next: (page) => {
        this.page.set(page);
        this.loading.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.loading.set(false);
      },
    });
  }
}
