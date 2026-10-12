import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
      .compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  // Creates the component and lets it send its first request
  function createApp() {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const request = http.expectOne((req) => req.url === '/api/comments');
    return { fixture, request, page: fixture.nativeElement as HTMLElement };
  }

  it('should render title', async () => {
    const { fixture, request, page } = createApp();
    request.flush({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
    await fixture.whenStable();

    expect(page.querySelector('h1')?.textContent).toContain('Comments');
  });

  it('asks for the first page, newest first', () => {
    const { request } = createApp();

    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('sortBy')).toBe('createdAt');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
  });

  it('shows how many comments the API has', async () => {
    const { fixture, request, page } = createApp();
    request.flush({ items: [], page: 1, pageSize: 25, totalCount: 42, totalPages: 2 });
    await fixture.whenStable();

    expect(page.textContent).toContain('Total comments: 42');
  });

  it('shows a message when the API is not reachable', async () => {
    const { fixture, request, page } = createApp();
    request.flush('error', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(page.textContent).toContain('Could not load comments');
  });
});
