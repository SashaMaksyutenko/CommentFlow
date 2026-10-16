import { Component, inject, signal } from '@angular/core';
import { CommentDialog } from './comments/comment-dialog/comment-dialog';
import { CommentsTable } from './comments/comments-table/comments-table';
import { CommentDialogService } from './services/comment-dialog.service';

@Component({
  imports: [CommentDialog, CommentsTable],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Comments');

  // "Add a comment" opens the window with the form
  protected readonly dialog = inject(CommentDialogService);
}
