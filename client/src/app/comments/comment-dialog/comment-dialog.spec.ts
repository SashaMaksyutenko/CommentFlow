import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentModel } from '../../models/comment.model';
import { CommentDialogService } from '../../services/comment-dialog.service';
import { CommentDialog } from './comment-dialog';

describe('CommentDialog', () => {
  let fixture: ComponentFixture<CommentDialog>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let dialog: CommentDialogService;

  const answered: CommentModel = {
    id: 7,
    parentId: null,
    userName: 'Olena2',
    email: 'olena@example.com',
    homePage: null,
    text: 'Hello',
    createdAt: '2026-10-01T10:27:00Z',
    attachment: null,
    replies: [],
  };

  function overlay(): HTMLElement | null {
    return element.querySelector('.overlay');
  }

  // Opens the window and answers the CAPTCHA request of the form inside it
  async function open(how: () => void): Promise<void> {
    how();
    await fixture.whenStable();
    http.expectOne('/api/captcha').flush({ id: 'captcha-id', image: 'data:image/png;base64,AAAA' });
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentDialog],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    dialog = TestBed.inject(CommentDialogService);
    fixture = TestBed.createComponent(CommentDialog);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('shows nothing while it is closed', () => {
    expect(overlay()).toBeNull();
    expect(element.querySelector('app-comment-form')).toBeNull();
  });

  it('shows the form in a window and takes the keyboard focus for a new comment', async () => {
    await open(() => dialog.openNew());

    expect(overlay()?.querySelector('[role="dialog"] app-comment-form')).not.toBeNull();
    expect(document.activeElement).toBe(element.querySelector('.window'));
  });

  it('opens with the reply mode and the cursor in the text field for an answer', async () => {
    await open(() => dialog.openReply(answered));

    expect(element.querySelector('.replying')?.textContent).toContain('Replying to Olena2');
    expect(document.activeElement).toBe(element.querySelector('#text'));
  });

  it('closes with the close button', async () => {
    await open(() => dialog.openNew());

    element.querySelector<HTMLButtonElement>('.close')!.click();
    await fixture.whenStable();

    expect(dialog.isOpen()).toBe(false);
    expect(overlay()).toBeNull();
  });

  it('closes with Escape and with a click on the dark area, but not with a click inside', async () => {
    await open(() => dialog.openNew());
    element.querySelector<HTMLElement>('.window')!.click();
    await fixture.whenStable();
    expect(dialog.isOpen()).toBe(true);

    element.querySelector('.window')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();
    expect(dialog.isOpen()).toBe(false);

    await open(() => dialog.openNew());
    overlay()!.click();
    await fixture.whenStable();
    expect(dialog.isOpen()).toBe(false);
  });
});
