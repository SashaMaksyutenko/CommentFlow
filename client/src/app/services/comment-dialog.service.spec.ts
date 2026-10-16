import { TestBed } from '@angular/core/testing';
import { CommentModel } from '../models/comment.model';
import { CommentDialogService } from './comment-dialog.service';
import { ReplyTargetService } from './reply-target.service';

describe('CommentDialogService', () => {
  const comment: CommentModel = {
    id: 7,
    parentId: null,
    userName: 'Olena2',
    email: 'olena@example.com',
    homePage: null,
    text: 'Hello',
    createdAt: '2026-10-01T10:27:00Z',
    attachment: null,
    replies: [],
  };

  let dialog: CommentDialogService;
  let replyTarget: ReplyTargetService;

  beforeEach(() => {
    dialog = TestBed.inject(CommentDialogService);
    replyTarget = TestBed.inject(ReplyTargetService);
  });

  it('is closed at the start', () => {
    expect(dialog.isOpen()).toBe(false);
  });

  it('opens for an answer and tells the form whom the visitor answers', () => {
    dialog.openReply(comment);

    expect(dialog.isOpen()).toBe(true);
    expect(replyTarget.target()).toBe(comment);
  });

  it('opens for a new comment and forgets an answer that was not sent', () => {
    dialog.openReply(comment);
    dialog.close();

    dialog.openNew();

    expect(dialog.isOpen()).toBe(true);
    expect(replyTarget.target()).toBeNull();
  });

  it('closes', () => {
    dialog.openNew();

    dialog.close();

    expect(dialog.isOpen()).toBe(false);
  });
});
