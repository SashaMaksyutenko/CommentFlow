import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      // The comments table inside the page asks the API, so HTTP is replaced here too
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
      .compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  it('shows the title and the comments table', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    http
      .expectOne((req) => req.url === '/api/comments')
      .flush({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('h1')?.textContent).toContain('Comments');
    expect(page.querySelector('app-comments-table')).not.toBeNull();
  });
});
