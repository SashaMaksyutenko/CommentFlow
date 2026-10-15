import { formatDate } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentModel } from '../../models/comment.model';
import { CommentEventsService } from '../../services/comment-events.service';
import { ReplyTargetService } from '../../services/reply-target.service';
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

  // The request the component has just sent
  function lastRequest(): TestRequest {
    return http.expectOne((req) => req.url === '/api/comments');
  }

  // Answers the request of the component the way the API would
  async function apiReturns(items: CommentModel[], page = 1, totalPages = 1): Promise<void> {
    lastRequest().flush({ items, page, pageSize: 25, totalCount: items.length, totalPages });
    await fixture.whenStable();
  }

  // A button in the table header or under the table, found by its text
  function button(label: string): HTMLButtonElement {
    const buttons = Array.from(element.querySelectorAll('button'));
    return buttons.find((item) => item.textContent?.includes(label))!;
  }

  function header(label: string): HTMLElement {
    return button(label).closest('th')!;
  }

  // The button that shows or hides the replies, by the number of the comment in the table
  function repliesButton(index: number): HTMLButtonElement {
    return element.querySelectorAll<HTMLButtonElement>('.replies-toggle')[index];
  }

  function clickReplies(index: number): void {
    repliesButton(index).click();
    fixture.detectChanges(); // the replies component appears (or goes away) right now
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

  it('puts an avatar with the first letter in front of the user name', async () => {
    await apiReturns([comment({ id: 1, userName: 'olena2' })]);

    const author = element.querySelector('tbody tr .author')!;
    expect(author.querySelector('app-user-avatar')?.textContent?.trim()).toBe('O');
    expect(author.textContent).toContain('olena2');
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

    const cell = element.querySelector('.text')!;
    expect(cell.querySelector('i')?.textContent).toBe('all');
    expect(cell.querySelector('script')).toBeNull();
    expect(cell.querySelector('[onerror]')).toBeNull();
  });

  it('shows the file of a comment under its text', async () => {
    const attachment: CommentModel['attachment'] = {
      url: '/uploads/abc.png',
      fileName: 'photo.png',
      type: 'Image',
      status: 'Ready',
      contentType: 'image/png',
      size: 41165,
    };

    await apiReturns([comment({ id: 1, attachment }), comment({ id: 2, attachment: null })]);

    const rows = element.querySelectorAll('tbody tr');
    expect(rows[0].querySelector('app-attachment-view img')?.getAttribute('src')).toBe('/uploads/abc.png');
    expect(rows[1].querySelector('app-attachment-view')).toBeNull();
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

  it('sorts by a column from A to Z when its header is clicked', async () => {
    await apiReturns([comment()]);

    button('User Name').click();

    const request = lastRequest().request;
    expect(request.params.get('sortBy')).toBe('userName');
    expect(request.params.get('sortDirection')).toBe('asc');
  });

  it('turns the order around when the same header is clicked again', async () => {
    await apiReturns([comment()]);
    button('E-mail').click();
    await apiReturns([comment()]);

    button('E-mail').click();

    const request = lastRequest().request;
    expect(request.params.get('sortBy')).toBe('email');
    expect(request.params.get('sortDirection')).toBe('desc');
  });

  it('shows the oldest comments first after a click on Date', async () => {
    await apiReturns([comment()]);

    // Date is already sorted from the newest, so the click turns it around
    button('Date').click();

    expect(lastRequest().request.params.get('sortDirection')).toBe('asc');
  });

  it('marks the column the table is sorted by', async () => {
    await apiReturns([comment()]);
    expect(header('Date').getAttribute('aria-sort')).toBe('descending');
    expect(header('Date').textContent).toContain('▼');

    button('User Name').click();
    await apiReturns([comment()]);

    expect(header('User Name').getAttribute('aria-sort')).toBe('ascending');
    expect(header('User Name').textContent).toContain('▲');
    expect(header('Date').hasAttribute('aria-sort')).toBe(false);
  });

  it('asks for the next and the previous page and keeps the order', async () => {
    await apiReturns([comment()], 1, 3);

    button('Next').click();
    const next = lastRequest();
    expect(next.request.params.get('page')).toBe('2');
    expect(next.request.params.get('sortBy')).toBe('createdAt');
    next.flush({ items: [comment()], page: 2, pageSize: 25, totalCount: 60, totalPages: 3 });
    await fixture.whenStable();
    expect(element.querySelector('.pagination')?.textContent).toContain('Page 2 of 3');

    button('Previous').click();

    expect(lastRequest().request.params.get('page')).toBe('1');
  });

  it('turns off Previous on the first page and Next on the last one', async () => {
    await apiReturns([comment()], 1, 2);
    expect(button('Previous').disabled).toBe(true);
    expect(button('Next').disabled).toBe(false);

    button('Next').click();
    await apiReturns([comment()], 2, 2);

    expect(button('Previous').disabled).toBe(false);
    expect(button('Next').disabled).toBe(true);
  });

  it('hides the page buttons when all comments fit on one page', async () => {
    await apiReturns([comment()]);

    expect(element.querySelector('.pagination')).toBeNull();
  });

  it('goes back to the first page when the order changes', async () => {
    await apiReturns([comment()], 1, 3);
    button('Next').click();
    await apiReturns([comment()], 2, 3);

    button('User Name').click();

    expect(lastRequest().request.params.get('page')).toBe('1');
  });

  it('keeps the old rows on the screen while the new ones are loading', async () => {
    await apiReturns([comment()], 1, 2);

    button('Next').click();
    fixture.detectChanges();

    expect(element.querySelectorAll('tbody tr').length).toBe(1);
    expect(element.querySelector('table')?.getAttribute('aria-busy')).toBe('true');
    await apiReturns([comment()], 2, 2);
    expect(element.querySelector('table')?.getAttribute('aria-busy')).toBe('false');
  });

  it('drops the request that is still running when the visitor clicks again', async () => {
    await apiReturns([comment()], 1, 3);
    button('Next').click();
    const first = lastRequest();

    button('User Name').click();

    expect(first.cancelled).toBe(true);
    expect(lastRequest().request.params.get('sortBy')).toBe('userName');
  });

  it('does not load the replies until the visitor asks for them', async () => {
    await apiReturns([comment({ id: 1 })]);

    expect(element.querySelector('.replies-row')).toBeNull();
    expect(repliesButton(0).getAttribute('aria-expanded')).toBe('false');
    http.expectNone('/api/comments/1/replies');
  });

  it('loads the replies and shows them right under the comment', async () => {
    await apiReturns([comment({ id: 1 }), comment({ id: 2 }), comment({ id: 3 })]);

    clickReplies(1);
    http.expectOne('/api/comments/2/replies').flush([comment({ id: 9, parentId: 2, text: 'A reply' })]);
    await fixture.whenStable();

    // 3 comments and one more row with the replies, after the second comment
    const rows = element.querySelectorAll('tbody tr');
    expect(rows.length).toBe(4);
    expect(rows[2].classList.contains('replies-row')).toBe(true);
    expect(rows[2].textContent).toContain('A reply');
    expect(repliesButton(1).textContent).toContain('Hide replies');
    expect(repliesButton(1).getAttribute('aria-expanded')).toBe('true');
  });

  it('hides the replies when the button is clicked again', async () => {
    await apiReturns([comment({ id: 1 })]);
    clickReplies(0);
    http.expectOne('/api/comments/1/replies').flush([]);
    await fixture.whenStable();

    clickReplies(0);

    expect(element.querySelector('.replies-row')).toBeNull();
    expect(repliesButton(0).textContent).toContain('Show replies');
    expect(repliesButton(0).getAttribute('aria-expanded')).toBe('false');
  });

  it('loads the shown page again when a top-level comment is created', async () => {
    await apiReturns([comment({ id: 1 })], 1, 3);
    button('Next').click();
    await apiReturns([comment({ id: 1 })], 2, 3);

    TestBed.inject(CommentEventsService).notifyOwnComment(comment({ id: 7, parentId: null }));

    // Still the second page: the visitor stays where they were
    expect(lastRequest().request.params.get('page')).toBe('2');
  });

  it('loads the page again for a top-level comment of someone else too', async () => {
    await apiReturns([comment({ id: 1 })]);

    TestBed.inject(CommentEventsService).notifyOtherComment(comment({ id: 7, parentId: null }));

    expect(lastRequest().request.params.get('page')).toBe('1');
  });

  it('shows a processed image of a comment without loading the page again', async () => {
    const attachment: CommentModel['attachment'] = {
      url: '/uploads/abc.png',
      fileName: 'photo.png',
      type: 'Image',
      status: 'Processing',
      contentType: 'image/png',
      size: 50000,
    };
    await apiReturns([comment({ id: 1 }), comment({ id: 2, attachment })]);
    expect(element.textContent).toContain('is being processed');

    TestBed.inject(CommentEventsService).notifyAttachmentProcessed(2, { ...attachment!, status: 'Ready' });
    await fixture.whenStable();

    // No request: afterEach checks that nothing else was sent
    const rows = element.querySelectorAll('tbody tr');
    expect(rows[1].querySelector('app-attachment-view img')?.getAttribute('src')).toBe('/uploads/abc.png');
    expect(element.textContent).not.toContain('is being processed');
  });

  it('does nothing when a reply to a comment from another page is created', async () => {
    await apiReturns([comment({ id: 1 })]);

    TestBed.inject(CommentEventsService).notifyOwnComment(comment({ id: 7, parentId: 99 }));
    fixture.detectChanges();

    // No new page and no replies: afterEach checks that nothing was requested
    expect(element.querySelector('.replies-row')).toBeNull();
  });

  it('does not open the replies of a row when someone else answers it', async () => {
    await apiReturns([comment({ id: 1 }), comment({ id: 2 })]);

    TestBed.inject(CommentEventsService).notifyOtherComment(comment({ id: 9, parentId: 2 }));
    fixture.detectChanges();

    expect(element.querySelector('.replies-row')).toBeNull();
    expect(repliesButton(1).getAttribute('aria-expanded')).toBe('false');
  });

  it('opens the replies of a comment when the visitor answers it', async () => {
    await apiReturns([comment({ id: 1 }), comment({ id: 2 })]);

    TestBed.inject(CommentEventsService).notifyOwnComment(comment({ id: 9, parentId: 2 }));
    fixture.detectChanges();
    http.expectOne('/api/comments/2/replies').flush([comment({ id: 9, parentId: 2, text: 'A new reply' })]);
    await fixture.whenStable();

    expect(repliesButton(1).getAttribute('aria-expanded')).toBe('true');
    expect(element.querySelector('.replies-row')?.textContent).toContain('A new reply');
  });

  it('tells the comment form which comment the visitor wants to answer', async () => {
    const second = comment({ id: 2, userName: 'Olena2' });
    await apiReturns([comment({ id: 1 }), second]);

    element.querySelectorAll<HTMLButtonElement>('.reply-button')[1].click();

    expect(TestBed.inject(ReplyTargetService).target()).toEqual(second);
  });

  it('can show the replies of several comments at once', async () => {
    await apiReturns([comment({ id: 1 }), comment({ id: 2 })]);

    clickReplies(0);
    clickReplies(1);
    http.expectOne('/api/comments/1/replies').flush([]);
    http.expectOne('/api/comments/2/replies').flush([]);
    await fixture.whenStable();

    expect(element.querySelectorAll('.replies-row').length).toBe(2);
  });
});
