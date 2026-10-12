import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommentModel } from '../../models/comment.model';
import { CommentsQuery, PagedResponse } from '../../models/paging.model';
import { CommentsApiService } from '../../services/comments-api.service';

@Component({
  imports: [DatePipe],
  selector: 'app-comments-table',
  styleUrl: './comments-table.css',
  templateUrl: './comments-table.html',
})
export class CommentsTable implements OnInit, OnDestroy {
  private readonly api = inject(CommentsApiService);

  // The request that is running now, if any
  private request?: Subscription;

  // Newest first by default (LIFO)
  protected readonly query = signal<CommentsQuery>({ page: 1, sortBy: 'createdAt', sortDirection: 'desc' });

  // null until the first page is loaded
  protected readonly page = signal<PagedResponse<CommentModel> | null>(null);
  protected readonly loading = signal(false);
  protected readonly loadFailed = signal(false);

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
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
