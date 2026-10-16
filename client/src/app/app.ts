import { Component, inject, signal } from '@angular/core';
import { AddedNotice } from './comments/added-notice/added-notice';
import { CommentDialog } from './comments/comment-dialog/comment-dialog';
import { CommentsTable } from './comments/comments-table/comments-table';
import { CommentDialogService } from './services/comment-dialog.service';

@Component({
  imports: [AddedNotice, CommentDialog, CommentsTable],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Comments');

  // "Add a comment" opens the window with the form
  protected readonly dialog = inject(CommentDialogService);
}
