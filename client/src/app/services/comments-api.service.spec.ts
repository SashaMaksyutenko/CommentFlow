import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CommentModel } from '../models/comment.model';
import { NewComment } from '../models/new-comment.model';
import { PagedResponse } from '../models/paging.model';
import { CommentsApiService } from './comments-api.service';

describe('CommentsApiService', () => {
  let service: CommentsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      // The testing provider catches requests, nothing goes to a real server
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CommentsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  // Fails the test if some request was made but not expected
  afterEach(() => http.verify());

  it('asks the API for the page with the given sorting', () => {
    service.getComments({ page: 2, sortBy: 'userName', sortDirection: 'asc' }).subscribe();

    const request = http.expectOne((req) => req.url === '/api/comments');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('sortBy')).toBe('userName');
    expect(request.request.params.get('sortDirection')).toBe('asc');
    request.flush({ items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 });
  });

  it('returns the page from the API as it is', () => {
    const comment: CommentModel = {
      id: 7,
      parentId: null,
      userName: 'Sasha1',
      email: 'sasha@example.com',
      homePage: null,
      text: 'Hi <i>all</i>',
      createdAt: '2026-10-01T10:27:00Z',
      attachment: null,
      replies: [],
    };
    const page: PagedResponse<CommentModel> = { items: [comment], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 };
    let result: PagedResponse<CommentModel> | undefined;

    service.getComments({ page: 1, sortBy: 'createdAt', sortDirection: 'desc' }).subscribe((r) => (result = r));
    http.expectOne((req) => req.url === '/api/comments').flush(page);

    expect(result).toEqual(page);
  });

  it('asks the API for the replies of the given comment', () => {
    const reply: CommentModel = {
      id: 8,
      parentId: 7,
      userName: 'Olena2',
      email: 'olena@example.com',
      homePage: null,
      text: 'Hello',
      createdAt: '2026-10-01T10:30:00Z',
      attachment: null,
      replies: [],
    };
    let result: CommentModel[] | undefined;

    service.getReplies(7).subscribe((r) => (result = r));
    const request = http.expectOne('/api/comments/7/replies');
    request.flush([reply]);

    expect(request.request.method).toBe('GET');
    expect(result).toEqual([reply]);
  });

  it('loads a TXT attachment as plain text', () => {
    let result: string | undefined;

    service.getAttachmentText('/uploads/abc.txt').subscribe((r) => (result = r));
    const request = http.expectOne('/uploads/abc.txt');
    request.flush('Hello\nfrom a file');

    expect(request.request.responseType).toBe('text');
    expect(result).toBe('Hello\nfrom a file');
  });

  it('asks the API how the text will look and returns only the text', () => {
    let result: string | undefined;

    service.previewComment('Hi <i>all</i> & you').subscribe((r) => (result = r));
    const request = http.expectOne('/api/comments/preview');
    request.flush({ text: 'Hi <i>all</i> &amp; you' });

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ text: 'Hi <i>all</i> & you' });
    expect(result).toBe('Hi <i>all</i> &amp; you');
  });

  describe('createComment', () => {
    const newComment: NewComment = {
      userName: 'Sasha1',
      email: 'sasha@example.com',
      homePage: ' https://example.com ',
      text: 'Hi <i>all</i>',
      parentId: null,
      file: null,
      captchaId: 'captcha-id',
      captchaAnswer: 'ABC123',
    };

    it('posts the comment as form data', () => {
      service.createComment(newComment).subscribe();

      const request = http.expectOne('/api/comments');
      const body = request.request.body as FormData;
      expect(request.request.method).toBe('POST');
      expect(body.get('userName')).toBe('Sasha1');
      expect(body.get('email')).toBe('sasha@example.com');
      expect(body.get('homePage')).toBe('https://example.com');
      expect(body.get('text')).toBe('Hi <i>all</i>');
      expect(body.get('captchaId')).toBe('captcha-id');
      expect(body.get('captchaAnswer')).toBe('ABC123');
      request.flush({});
    });

    it('leaves the home page out when it is empty', () => {
      service.createComment({ ...newComment, homePage: '' }).subscribe();

      const request = http.expectOne('/api/comments');
      expect((request.request.body as FormData).has('homePage')).toBe(false);
      request.flush({});
    });

    it('sends the file with its name only when one is attached', () => {
      service.createComment(newComment).subscribe();
      const withoutFile = http.expectOne('/api/comments');
      expect((withoutFile.request.body as FormData).has('file')).toBe(false);
      withoutFile.flush({});

      const photo = new File(['picture'], 'photo.png', { type: 'image/png' });
      service.createComment({ ...newComment, file: photo }).subscribe();
      const withFile = http.expectOne('/api/comments');
      const sent = (withFile.request.body as FormData).get('file') as File;
      expect(sent.name).toBe('photo.png');
      expect(sent.size).toBe(photo.size);
      withFile.flush({});
    });

    it('sends the id of the parent only for a reply', () => {
      service.createComment(newComment).subscribe();
      const topLevel = http.expectOne('/api/comments');
      expect((topLevel.request.body as FormData).has('parentId')).toBe(false);
      topLevel.flush({});

      service.createComment({ ...newComment, parentId: 7 }).subscribe();
      const reply = http.expectOne('/api/comments');
      expect((reply.request.body as FormData).get('parentId')).toBe('7');
      reply.flush({});
    });
  });
});
