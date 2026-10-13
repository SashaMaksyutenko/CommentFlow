import { TestBed } from '@angular/core/testing';
import { CommentModel } from '../models/comment.model';
import { CommentEventsService } from './comment-events.service';

describe('CommentEventsService', () => {
  const comment: CommentModel = {
    id: 5,
    parentId: null,
    userName: 'Sasha1',
    email: 'sasha@example.com',
    homePage: null,
    text: 'Hello',
    createdAt: '2026-10-01T10:27:00Z',
    attachment: null,
    replies: [],
  };

  it('gives a created comment to everyone who listens', () => {
    const events = TestBed.inject(CommentEventsService);
    const first: CommentModel[] = [];
    const second: CommentModel[] = [];
    events.created$.subscribe((created) => first.push(created));
    events.created$.subscribe((created) => second.push(created));

    events.notifyCreated(comment);

    expect(first).toEqual([comment]);
    expect(second).toEqual([comment]);
  });

  it('does not repeat old comments to someone who starts listening later', () => {
    const events = TestBed.inject(CommentEventsService);
    const received: CommentModel[] = [];

    events.notifyCreated(comment);
    events.created$.subscribe((created) => received.push(created));

    expect(received).toEqual([]);
  });
});
