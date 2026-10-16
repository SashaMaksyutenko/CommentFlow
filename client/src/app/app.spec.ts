import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      // The comments table and the form inside the page ask the API, so HTTP is replaced here too
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
      .compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // Shows the page and answers the request of the comments table
  async function openPage(): Promise<ComponentFixture<App>> {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    http
      .expectOne((req) => req.url === '/api/comments')
      .flush({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
    await fixture.whenStable();
    return fixture;
  }

  it('shows the title, the Add a comment button and the comments, but not the form', async () => {
    const page: HTMLElement = (await openPage()).nativeElement;

    expect(page.querySelector('h1')?.textContent).toContain('Comments');
    expect(page.querySelector('.page-head button')?.textContent).toContain('Add a comment');
    expect(page.querySelector('app-comments-table')).not.toBeNull();
    // No CAPTCHA request either: afterEach checks that nothing else was asked
    expect(page.querySelector('app-comment-form')).toBeNull();
  });

  it('opens the form in a window after a click on Add a comment', async () => {
    const fixture = await openPage();
    const page: HTMLElement = fixture.nativeElement;

    page.querySelector<HTMLButtonElement>('.page-head button')!.click();
    await fixture.whenStable();
    http.expectOne('/api/captcha').flush({ id: 'captcha-id', image: 'data:image/png;base64,AAAA' });
    await fixture.whenStable();

    expect(page.querySelector('app-comment-dialog [role="dialog"] app-comment-form')).not.toBeNull();
  });
});
