import { Component, OnInit, inject, signal } from '@angular/core';
import { CommentsApiService } from './services/comments-api.service';

@Component({
  imports: [],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly api = inject(CommentsApiService);

  protected readonly title = signal('Comments');

  // null until the first answer from the API
  protected readonly totalCount = signal<number | null>(null);
  protected readonly loadFailed = signal(false);

  ngOnInit(): void {
    this.api.getComments({ page: 1, sortBy: 'createdAt', sortDirection: 'desc' }).subscribe({
      next: (page) => this.totalCount.set(page.totalCount),
      error: () => this.loadFailed.set(true),
    });
  }
}
