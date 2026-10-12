import { Component, signal } from '@angular/core';
import { CommentForm } from './comments/comment-form/comment-form';
import { CommentsTable } from './comments/comments-table/comments-table';

@Component({
  imports: [CommentForm, CommentsTable],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Comments');
}
