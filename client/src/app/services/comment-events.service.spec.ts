import { TestBed } from '@angular/core/testing';
import { AttachmentModel, CommentModel } from '../models/comment.model';
import { CommentEventsService, CreatedComment, ProcessedAttachment } from './comment-events.service';

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
    const first: CreatedComment[] = [];
    const second: CreatedComment[] = [];
    events.created$.subscribe((created) => first.push(created));
    events.created$.subscribe((created) => second.push(created));

    events.notifyOwnComment(comment);

    expect(first).toEqual([{ comment, own: true }]);
    expect(second).toEqual([{ comment, own: true }]);
  });

  it('marks a comment written by someone else', () => {
    const events = TestBed.inject(CommentEventsService);
    const received: CreatedComment[] = [];
    events.created$.subscribe((created) => received.push(created));

    events.notifyOtherComment(comment);

    expect(received).toEqual([{ comment, own: false }]);
  });

  it('tells about a processed image of a comment', () => {
    const events = TestBed.inject(CommentEventsService);
    const received: ProcessedAttachment[] = [];
    events.attachmentProcessed$.subscribe((processed) => received.push(processed));
    const attachment: AttachmentModel = {
      url: '/uploads/abc.png',
      fileName: 'photo.png',
      type: 'Image',
      status: 'Ready',
      contentType: 'image/png',
      size: 41165,
    };

    events.notifyAttachmentProcessed(5, attachment);

    expect(received).toEqual([{ commentId: 5, attachment }]);
  });

  it('does not repeat old comments to someone who starts listening later', () => {
    const events = TestBed.inject(CommentEventsService);
    const received: CreatedComment[] = [];

    events.notifyOwnComment(comment);
    events.created$.subscribe((created) => received.push(created));

    expect(received).toEqual([]);
  });
});
