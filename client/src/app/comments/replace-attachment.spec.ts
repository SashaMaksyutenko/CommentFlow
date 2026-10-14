import { AttachmentModel, CommentModel } from '../models/comment.model';
import { replaceAttachment } from './replace-attachment';

describe('replaceAttachment', () => {
  const processing: AttachmentModel = {
    url: '/uploads/abc.png',
    fileName: 'photo.png',
    type: 'Image',
    status: 'Processing',
    contentType: 'image/png',
    size: 50000,
  };
  const ready: AttachmentModel = { ...processing, status: 'Ready', size: 41165 };

  function comment(id: number, replies: CommentModel[] = [], attachment: AttachmentModel | null = null): CommentModel {
    return {
      id,
      parentId: null,
      userName: 'Sasha1',
      email: 'sasha@example.com',
      homePage: null,
      text: 'Hello',
      createdAt: '2026-10-01T10:27:00Z',
      attachment,
      replies,
    };
  }

  it('gives the new attachment to the comment with this id in a flat list', () => {
    const comments = [comment(1), comment(2, [], processing)];

    const result = replaceAttachment(comments, 2, ready);

    expect(result[1].attachment).toEqual(ready);
    expect(result[0].attachment).toBeNull();
  });

  it('finds the comment deep in a tree of replies', () => {
    const tree = [comment(1, [comment(2, [comment(3, [], processing)])])];

    const result = replaceAttachment(tree, 3, ready);

    expect(result[0].replies[0].replies[0].attachment).toEqual(ready);
  });

  it('does not change the comments it was given', () => {
    const comments = [comment(1, [], processing)];

    const result = replaceAttachment(comments, 1, ready);

    expect(comments[0].attachment).toEqual(processing);
    expect(result).not.toBe(comments);
  });

  it('changes nothing when there is no comment with this id', () => {
    const comments = [comment(1, [comment(2)], processing)];

    const result = replaceAttachment(comments, 99, ready);

    expect(result).toEqual(comments);
  });
});
