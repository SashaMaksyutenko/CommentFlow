import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { CommentModel } from '../../models/comment.model';

// Shows replies as a tree. Every reply can have its own replies,
// and they are drawn by the same component, one level deeper.
@Component({
  imports: [DatePipe],
  selector: 'app-reply-list',
  styleUrl: './reply-list.css',
  templateUrl: './reply-list.html',
})
export class ReplyList {
  readonly replies = input.required<CommentModel[]>();
}
