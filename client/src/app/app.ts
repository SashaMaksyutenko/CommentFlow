import { Component, signal } from '@angular/core';
import { CommentsTable } from './comments/comments-table/comments-table';

@Component({
  imports: [CommentsTable],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Comments');
}
