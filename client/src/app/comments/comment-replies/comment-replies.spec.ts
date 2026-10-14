import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AttachmentModel, CommentModel } from '../../models/comment.model';
import { CommentEventsService } from '../../services/comment-events.service';
import { CommentReplies } from './comment-replies';

describe('CommentReplies', () => {
  let fixture: ComponentFixture<CommentReplies>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // A valid reply; a test changes only the fields it cares about
  function reply(changes: Partial<CommentModel> = {}): CommentModel {
    return {
      id: 8,
      parentId: 7,
      userName: 'Olena2',
      email: 'olena@example.com',
      homePage: null,
      text: 'Hello',
      createdAt: '2026-10-01T10:30:00Z',
      attachment: null,
      replies: [],
      ...changes,
    };
  }

  // Answers the request of the component the way the API would
  async function apiReturns(replies: CommentModel[]): Promise<void> {
    http.expectOne('/api/comments/7/replies').flush(replies);
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentReplies],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CommentReplies);
    element = fixture.nativeElement;
    fixture.componentRef.setInput('commentId', 7);
    fixture.detectChanges(); // the component starts and sends its request
  });

  afterEach(() => http.verify());

  it('asks the API for the replies of its comment', () => {
    const request = http.expectOne('/api/comments/7/replies');

    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('shows "Loading" until the API answers', async () => {
    expect(element.textContent).toContain('Loading replies');

    await apiReturns([reply()]);

    expect(element.textContent).not.toContain('Loading replies');
  });

  it('shows the replies as a tree', async () => {
    const nested = reply({ id: 9, parentId: 8, text: 'Nested' });

    await apiReturns([reply({ id: 8, text: 'First', replies: [nested] })]);

    const texts = Array.from(element.querySelectorAll('.text')).map((item) => item.textContent);
    expect(texts).toEqual(['First', 'Nested']);
    expect(element.querySelector('app-reply-list app-reply-list')).not.toBeNull();
  });

  it('says so when there are no replies', async () => {
    await apiReturns([]);

    expect(element.textContent).toContain('No replies yet');
    expect(element.querySelector('app-reply-list')).toBeNull();
  });

  it('shows an error when the API fails', async () => {
    http.expectOne('/api/comments/7/replies').flush('error', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Could not load replies');
  });

  it('loads the replies again when a reply is created and keeps the old ones meanwhile', async () => {
    await apiReturns([reply({ id: 8, text: 'First' })]);

    TestBed.inject(CommentEventsService).notifyOwnComment(reply({ id: 9, parentId: 8 }));
    fixture.detectChanges();
    expect(element.textContent).toContain('First');
    expect(element.textContent).not.toContain('Loading replies');
    await apiReturns([reply({ id: 8, text: 'First', replies: [reply({ id: 9, parentId: 8, text: 'Second' })] })]);

    expect(element.textContent).toContain('Second');
  });

  it('loads the replies again for a reply of someone else too', async () => {
    await apiReturns([reply({ id: 8, text: 'First' })]);

    TestBed.inject(CommentEventsService).notifyOtherComment(reply({ id: 9, parentId: 8 }));

    await apiReturns([reply({ id: 8, text: 'First', replies: [reply({ id: 9, parentId: 8, text: 'From Olena' })] })]);
    expect(element.textContent).toContain('From Olena');
  });

  it('shows a processed image of a reply without loading the thread again', async () => {
    const attachment: AttachmentModel = {
      url: '/uploads/abc.png',
      fileName: 'photo.png',
      type: 'Image',
      status: 'Processing',
      contentType: 'image/png',
      size: 50000,
    };
    const nested = reply({ id: 9, parentId: 8, attachment });
    await apiReturns([reply({ id: 8, replies: [nested] })]);
    expect(element.textContent).toContain('is being processed');

    TestBed.inject(CommentEventsService).notifyAttachmentProcessed(9, { ...attachment, status: 'Ready' });
    await fixture.whenStable();

    // No request: afterEach checks that nothing else was sent
    expect(element.querySelector('app-attachment-view img')?.getAttribute('src')).toBe('/uploads/abc.png');
  });

  it('does not load the replies again when a top-level comment is created', async () => {
    await apiReturns([]);

    TestBed.inject(CommentEventsService).notifyOwnComment(reply({ id: 10, parentId: null }));

    http.expectNone('/api/comments/7/replies');
  });

  it('drops the request when the component is removed before the answer', () => {
    const request = http.expectOne('/api/comments/7/replies');

    fixture.destroy();

    expect(request.cancelled).toBe(true);
  });
});
