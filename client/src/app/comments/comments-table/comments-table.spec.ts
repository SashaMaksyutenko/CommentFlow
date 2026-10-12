import { formatDate } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentModel } from '../../models/comment.model';
import { CommentsTable } from './comments-table';

describe('CommentsTable', () => {
  let fixture: ComponentFixture<CommentsTable>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // A valid comment; a test changes only the fields it cares about
  function comment(changes: Partial<CommentModel> = {}): CommentModel {
    return {
      id: 1,
      parentId: null,
      userName: 'Sasha1',
      email: 'sasha@example.com',
      homePage: null,
      text: 'Hello',
      createdAt: '2026-10-01T10:27:00Z',
      attachment: null,
      replies: [],
      ...changes,
    };
  }

  // Answers the request of the component the way the API would
  async function apiReturns(items: CommentModel[]): Promise<void> {
    http
      .expectOne((req) => req.url === '/api/comments')
      .flush({ items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 });
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentsTable],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CommentsTable);
    element = fixture.nativeElement;
    fixture.detectChanges(); // the component starts and sends its request
  });

  afterEach(() => http.verify());

  it('asks for the first page, newest first', async () => {
    const request = http.expectOne((req) => req.url === '/api/comments');

    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('sortBy')).toBe('createdAt');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
  });

  it('shows "Loading" until the API answers', async () => {
    expect(element.textContent).toContain('Loading comments');

    await apiReturns([comment()]);

    expect(element.textContent).not.toContain('Loading comments');
  });

  it('shows a row for every comment', async () => {
    await apiReturns([
      comment({ id: 1, userName: 'Sasha1', email: 'sasha@example.com' }),
      comment({ id: 2, userName: 'Olena2', email: 'olena@example.com' }),
    ]);

    const rows = element.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Sasha1');
    expect(rows[0].textContent).toContain('sasha@example.com');
    expect(rows[1].textContent).toContain('Olena2');
  });

  it('shows the date in the visitor time zone as dd.MM.yy HH:mm', async () => {
    await apiReturns([comment({ createdAt: '2026-10-01T10:27:00Z' })]);

    const expected = formatDate('2026-10-01T10:27:00Z', 'dd.MM.yy HH:mm', 'en-US');
    expect(element.querySelector('tbody tr')?.textContent).toContain(expected);
  });

  it('makes the user name a link only when there is a home page', async () => {
    await apiReturns([
      comment({ id: 1, homePage: 'https://example.com/' }),
      comment({ id: 2, homePage: null }),
    ]);

    const rows = element.querySelectorAll('tbody tr');
    const link = rows[0].querySelector('td a');
    expect(link?.getAttribute('href')).toBe('https://example.com/');
    expect(link?.getAttribute('rel')).toContain('noopener');
    expect(rows[1].querySelector('td a')).toBeNull();
  });

  it('shows the allowed tags of the text and never lets scripts in', async () => {
    // The server already removes this, here we check the second line of defence in Angular
    const text = 'Hi <i>all</i> <script>alert(1)</script><img src="x" onerror="alert(1)">';

    await apiReturns([comment({ text })]);

    const cell = element.querySelector('td.text')!;
    expect(cell.querySelector('i')?.textContent).toBe('all');
    expect(cell.querySelector('script')).toBeNull();
    expect(cell.querySelector('[onerror]')).toBeNull();
  });

  it('says so when there are no comments', async () => {
    await apiReturns([]);

    expect(element.textContent).toContain('No comments yet');
    expect(element.querySelector('table')).toBeNull();
  });

  it('shows an error when the API fails', async () => {
    http.expectOne((req) => req.url === '/api/comments').flush('error', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Could not load comments');
  });
});
