import { DatePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { CommentModel } from '../../models/comment.model';
import { CommentDialogService } from '../../services/comment-dialog.service';
import { AttachmentView } from '../attachment-view/attachment-view';
import { UserAvatar } from '../user-avatar/user-avatar';

// Shows replies as a tree. Every reply can have its own replies,
// and they are drawn by the same component, one level deeper.
@Component({
  imports: [DatePipe, AttachmentView, UserAvatar],
  selector: 'app-reply-list',
  styleUrl: './reply-list.css',
  templateUrl: './reply-list.html',
})
export class ReplyList {
  // The Reply button opens the comment form for an answer to this reply
  protected readonly dialog = inject(CommentDialogService);

  readonly replies = input.required<CommentModel[]>();
}
