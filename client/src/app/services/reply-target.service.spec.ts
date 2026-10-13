import { TestBed } from '@angular/core/testing';
import { CommentModel } from '../models/comment.model';
import { ReplyTargetService } from './reply-target.service';

describe('ReplyTargetService', () => {
  const comment: CommentModel = {
    id: 7,
    parentId: null,
    userName: 'Sasha1',
    email: 'sasha@example.com',
    homePage: null,
    text: 'Hello',
    createdAt: '2026-10-01T10:27:00Z',
    attachment: null,
    replies: [],
  };

  it('has no comment to answer at the start', () => {
    expect(TestBed.inject(ReplyTargetService).target()).toBeNull();
  });

  it('remembers the comment the visitor wants to answer and forgets it on clear', () => {
    const replyTarget = TestBed.inject(ReplyTargetService);

    replyTarget.replyTo(comment);
    expect(replyTarget.target()).toBe(comment);

    replyTarget.clear();
    expect(replyTarget.target()).toBeNull();
  });
});
