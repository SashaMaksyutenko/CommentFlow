import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CommentModel } from '../models/comment.model';
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
});
