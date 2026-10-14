import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { notBlank } from '../comment-form/comment-validators';
import { CommentPreview } from './comment-preview';

describe('CommentPreview', () => {
  let fixture: ComponentFixture<CommentPreview>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // The control of the comment text, the way the form gives it
  let control: FormControl<string>;

  function previewButton(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('button')!;
  }

  // Presses Preview and returns the request the component has sent
  function pressPreview(): TestRequest {
    previewButton().click();
    fixture.detectChanges();
    return http.expectOne('/api/comments/preview');
  }

  function alertText(): string {
    return element.querySelector('[role="alert"]')?.textContent?.trim() ?? '';
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentPreview],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    control = new FormControl('', { nonNullable: true, validators: [notBlank] });

    fixture = TestBed.createComponent(CommentPreview);
    element = fixture.nativeElement;
    fixture.componentRef.setInput('control', control);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('sends the text to the API and shows the answer with its tags', async () => {
    control.setValue('Hi <strong>all</strong> & you');

    const request = pressPreview();
    expect(request.request.body).toEqual({ text: 'Hi <strong>all</strong> & you' });
    expect(previewButton().disabled).toBe(true);
    request.flush({ text: 'Hi <strong>all</strong> &amp; you' });
    await fixture.whenStable();

    const preview = element.querySelector('.preview .text')!;
    expect(preview.textContent).toBe('Hi all & you');
    expect(preview.querySelector('strong')?.textContent).toBe('all');
    expect(previewButton().disabled).toBe(false);
  });

  it('asks nothing for an empty text and lets the form show why', () => {
    previewButton().click();
    fixture.detectChanges();

    // No request: afterEach checks that nothing was sent
    expect(control.touched).toBe(true);
    expect(element.querySelector('.preview')).toBeNull();
  });

  it('puts a tag error of the server on the text and shows no preview', async () => {
    control.setValue('Hello <b>all</b>');

    pressPreview().flush(
      { status: 400, errors: { Text: ['Tag <b> is not allowed.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(control.getError('server')).toBe('Tag <b> is not allowed.');
    expect(control.touched).toBe(true);
    expect(element.querySelector('.preview')).toBeNull();
    expect(alertText()).toBe('');
  });

  it('removes the preview as soon as the text changes', async () => {
    control.setValue('Hello');
    pressPreview().flush({ text: 'Hello' });
    await fixture.whenStable();
    expect(element.querySelector('.preview')).not.toBeNull();

    control.setValue('Hello again');
    await fixture.whenStable();

    expect(element.querySelector('.preview')).toBeNull();
  });

  it('drops the answer for a text that was changed in the meantime', () => {
    control.setValue('Hello');
    const request = pressPreview();

    control.setValue('Hello again');

    expect(request.cancelled).toBe(true);
  });

  it('explains the limit when there are too many previews', async () => {
    control.setValue('Hello');

    pressPreview().flush(null, { status: 429, statusText: 'Too Many Requests' });
    await fixture.whenStable();

    expect(alertText()).toContain('Too many previews');
    expect(control.valid).toBe(true);
  });

  it('asks to try later when the server breaks', async () => {
    control.setValue('Hello');

    pressPreview().flush('error', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(alertText()).toBe('Could not make the preview. Try again later.');
  });
});
