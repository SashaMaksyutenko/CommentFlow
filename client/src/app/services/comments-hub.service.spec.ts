import { TestBed } from '@angular/core/testing';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { AttachmentModel, CommentModel } from '../models/comment.model';
import { CommentEventsService, CreatedComment, ProcessedAttachment } from './comment-events.service';
import { CommentsHubService } from './comments-hub.service';

describe('CommentsHubService', () => {
  // Stands in for the real connection: remembers the handlers and lets a test call them
  // the way the server would. Only the parts the service uses are here.
  class FakeConnection {
    readonly handlers = new Map<string, (...args: never[]) => void>();
    readonly start = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);

    on(methodName: string, handler: (...args: never[]) => void): void {
      this.handlers.set(methodName, handler);
    }
  }

  // What the server does: calls a client method by its name with these arguments
  function serverCalls(methodName: string, ...args: unknown[]): void {
    const handler = connection.handlers.get(methodName) as unknown as (...values: unknown[]) => void;
    handler(...args);
  }

  const comment: CommentModel = {
    id: 7,
    parentId: null,
    userName: 'Olena2',
    email: 'olena@example.com',
    homePage: null,
    text: 'Hello from another browser',
    createdAt: '2026-10-01T10:27:00Z',
    attachment: null,
    replies: [],
  };

  let connection: FakeConnection;
  let hub: CommentsHubService;

  beforeEach(() => {
    connection = new FakeConnection();
    // The service builds its connection when it is created, so the builder must give the fake one
    vi.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection as unknown as HubConnection);
    hub = TestBed.inject(CommentsHubService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it('starts the connection when the app asks it to', () => {
    hub.connect();

    expect(connection.start).toHaveBeenCalledTimes(1);
  });

  it('passes a comment from the server on as written by someone else', () => {
    const heard: CreatedComment[] = [];
    TestBed.inject(CommentEventsService).created$.subscribe((event) => heard.push(event));

    serverCalls('CommentCreated', comment);

    expect(heard).toEqual([{ comment, own: false }]);
  });

  it('passes on the news that an image of a comment is processed', () => {
    const heard: ProcessedAttachment[] = [];
    TestBed.inject(CommentEventsService).attachmentProcessed$.subscribe((event) => heard.push(event));
    const attachment: AttachmentModel = {
      url: '/uploads/abc.png',
      fileName: 'photo.png',
      type: 'Image',
      status: 'Ready',
      contentType: 'image/png',
      size: 41165,
    };

    serverCalls('AttachmentProcessed', 7, attachment);

    expect(heard).toEqual([{ commentId: 7, attachment }]);
  });

  it('tries the first connection again after 5 seconds when the server is not there yet', async () => {
    vi.useFakeTimers();
    connection.start.mockRejectedValueOnce(new Error('Failed to fetch'));

    hub.connect();
    // Lets the failed start finish, so the retry gets planned
    await vi.advanceTimersByTimeAsync(0);
    expect(connection.start).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(5000);

    expect(connection.start).toHaveBeenCalledTimes(2);
  });
});
