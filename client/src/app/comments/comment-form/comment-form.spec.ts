import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentModel } from '../../models/comment.model';
import { CommentEventsService } from '../../services/comment-events.service';
import { CommentForm } from './comment-form';

describe('CommentForm', () => {
  let fixture: ComponentFixture<CommentForm>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // Answers the request of the CAPTCHA field the way the API would
  async function apiReturnsCaptcha(image: string): Promise<void> {
    http.expectOne('/api/captcha').flush({ id: 'captcha-id', image });
    await fixture.whenStable();
  }

  function field(id: string): HTMLInputElement | HTMLTextAreaElement {
    return element.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${id}`)!;
  }

  // Types the text into a field and leaves the field, like a visitor would
  function fill(id: string, value: string): void {
    field(id).value = value;
    field(id).dispatchEvent(new Event('input'));
    field(id).dispatchEvent(new Event('blur'));
    fixture.detectChanges();
  }

  function submit(): void {
    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    fixture.detectChanges();
  }

  // The message under the field, empty when there is none
  function errorOf(id: string): string {
    return element.querySelector(`#${id}-error`)?.textContent?.trim() ?? '';
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentForm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CommentForm);
    element = fixture.nativeElement;
    fixture.detectChanges(); // the CAPTCHA field inside the form asks for a picture
    await apiReturnsCaptcha('data:image/png;base64,AAAA');
  });

  afterEach(() => http.verify());

  it('shows no errors before the visitor does anything', () => {
    expect(element.querySelectorAll('.error').length).toBe(0);
  });

  it('asks for the required fields when an empty form is sent', () => {
    submit();

    expect(errorOf('userName')).toBe('This field is required.');
    expect(errorOf('email')).toBe('This field is required.');
    expect(errorOf('text')).toBe('This field is required.');
    expect(errorOf('captchaAnswer')).toBe('This field is required.');
    expect(errorOf('homePage')).toBe('');
  });

  it('shows the CAPTCHA picture and the field for the answer', () => {
    const picture = element.querySelector('app-captcha-field img');

    expect(picture?.getAttribute('src')).toBe('data:image/png;base64,AAAA');
    expect(field('captchaAnswer')).not.toBeNull();
  });

  it('forgets the CAPTCHA error when the visitor asks for a new picture', async () => {
    submit();
    expect(errorOf('captchaAnswer')).not.toBe('');

    element.querySelector<HTMLButtonElement>('app-captcha-field button')!.click();
    await apiReturnsCaptcha('data:image/png;base64,BBBB');

    expect(errorOf('captchaAnswer')).toBe('');
  });

  it('shows the error of a field only after the visitor leaves it', () => {
    field('userName').value = 'Sasha 1';
    field('userName').dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(errorOf('userName')).toBe('');

    field('userName').dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(errorOf('userName')).toBe('Only Latin letters and digits are allowed.');
  });

  it('explains what is wrong with the e-mail, the home page and a text of spaces', () => {
    fill('email', 'sasha@example');
    fill('homePage', 'example.com');
    fill('text', '   ');

    expect(errorOf('email')).toBe('E-mail format is invalid.');
    expect(errorOf('homePage')).toBe('The address must start with http:// or https://.');
    expect(errorOf('text')).toBe('This field is required.');
  });

  it('does not let the text be longer than 5000 characters', () => {
    fill('text', 'a'.repeat(5001));
    expect(errorOf('text')).toBe('No more than 5000 characters.');

    fill('text', 'a'.repeat(5000));
    expect(errorOf('text')).toBe('');
  });

  it('removes the error when the field is corrected', () => {
    fill('userName', 'Саша');
    expect(errorOf('userName')).not.toBe('');

    fill('userName', 'Sasha1');

    expect(errorOf('userName')).toBe('');
  });

  describe('sending', () => {
    // What the API returns for a created comment
    const created: CommentModel = {
      id: 5,
      parentId: null,
      userName: 'Sasha1',
      email: 'sasha@example.com',
      homePage: 'https://example.com',
      text: 'Hello <strong>all</strong>',
      createdAt: '2026-10-01T10:27:00Z',
      attachment: null,
      replies: [],
    };

    function fillCorrectly(): void {
      fill('userName', 'Sasha1');
      fill('email', 'sasha@example.com');
      fill('homePage', 'https://example.com');
      fill('text', 'Hello <strong>all</strong>');
      fill('captchaAnswer', 'ABC123');
    }

    // The POST the form has just sent
    function postRequest(): TestRequest {
      return http.expectOne((req) => req.method === 'POST' && req.url === '/api/comments');
    }

    function submitButton(): HTMLButtonElement {
      return element.querySelector<HTMLButtonElement>('button[type="submit"]')!;
    }

    function captchaPicture(): string | null | undefined {
      return element.querySelector('app-captcha-field img')?.getAttribute('src');
    }

    it('sends nothing while the form has errors', () => {
      fillCorrectly();
      fill('email', 'sasha@example');

      submit();

      http.expectNone((req) => req.method === 'POST');
    });

    it('sends what the visitor typed together with the id of the CAPTCHA picture', () => {
      fillCorrectly();

      submit();

      const body = postRequest().request.body as FormData;
      expect(body.get('userName')).toBe('Sasha1');
      expect(body.get('email')).toBe('sasha@example.com');
      expect(body.get('homePage')).toBe('https://example.com');
      expect(body.get('text')).toBe('Hello <strong>all</strong>');
      expect(body.get('captchaId')).toBe('captcha-id');
      expect(body.get('captchaAnswer')).toBe('ABC123');
    });

    it('turns the button off until the server answers', async () => {
      fillCorrectly();
      submit();
      const request = postRequest();
      expect(submitButton().disabled).toBe(true);
      expect(submitButton().textContent).toContain('Sending');

      request.flush(created, { status: 201, statusText: 'Created' });
      await apiReturnsCaptcha('data:image/png;base64,BBBB');

      expect(submitButton().disabled).toBe(false);
      expect(submitButton().textContent).toContain('Add comment');
    });

    it('after success keeps the name, clears the text and shows a new CAPTCHA', async () => {
      fillCorrectly();
      submit();

      postRequest().flush(created, { status: 201, statusText: 'Created' });
      await apiReturnsCaptcha('data:image/png;base64,BBBB');

      expect(element.querySelector('[role="status"]')?.textContent).toContain('Your comment was added');
      expect(field('userName').value).toBe('Sasha1');
      expect(field('text').value).toBe('');
      expect(field('captchaAnswer').value).toBe('');
      expect(captchaPicture()).toBe('data:image/png;base64,BBBB');
      expect(element.querySelectorAll('.error').length).toBe(0);
    });

    it('tells the rest of the app about the created comment', async () => {
      const heard: CommentModel[] = [];
      TestBed.inject(CommentEventsService).created$.subscribe((comment) => heard.push(comment));
      fillCorrectly();
      submit();

      postRequest().flush(created, { status: 201, statusText: 'Created' });
      await apiReturnsCaptcha('data:image/png;base64,BBBB');

      expect(heard).toEqual([created]);
    });

    it('tells nothing when the server refuses the comment', async () => {
      const heard: CommentModel[] = [];
      TestBed.inject(CommentEventsService).created$.subscribe((comment) => heard.push(comment));
      fillCorrectly();
      submit();

      postRequest().flush('error', { status: 500, statusText: 'Server Error' });
      await apiReturnsCaptcha('data:image/png;base64,BBBB');

      expect(heard).toEqual([]);
    });

    it('when the server refuses, keeps the text and shows a message and a new CAPTCHA', async () => {
      fillCorrectly();
      submit();

      postRequest().flush({ errors: { CaptchaAnswer: ['Wrong captcha.'] } }, { status: 400, statusText: 'Bad Request' });
      await apiReturnsCaptcha('data:image/png;base64,BBBB');

      expect(element.querySelector('[role="alert"]')?.textContent).toContain('Could not send the comment');
      expect(field('text').value).toBe('Hello <strong>all</strong>');
      expect(field('captchaAnswer').value).toBe('');
      expect(captchaPicture()).toBe('data:image/png;base64,BBBB');
    });
  });
});
