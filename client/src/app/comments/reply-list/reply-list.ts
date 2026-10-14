import { DatePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { CommentModel } from '../../models/comment.model';
import { ReplyTargetService } from '../../services/reply-target.service';
import { AttachmentView } from '../attachment-view/attachment-view';

// Shows replies as a tree. Every reply can have its own replies,
// and they are drawn by the same component, one level deeper.
@Component({
  imports: [DatePipe, AttachmentView],
  selector: 'app-reply-list',
  styleUrl: './reply-list.css',
  templateUrl: './reply-list.html',
})
export class ReplyList {
  // The Reply button tells the comment form whom the visitor answers
  protected readonly replyTarget = inject(ReplyTargetService);

  readonly replies = input.required<CommentModel[]>();
}
