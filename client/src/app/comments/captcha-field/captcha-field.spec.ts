import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { CaptchaField } from './captcha-field';

describe('CaptchaField', () => {
  let fixture: ComponentFixture<CaptchaField>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // The two controls a form would give to the field
  let idControl: FormControl<string>;
  let answerControl: FormControl<string>;

  // Answers the request of the component the way the API would
  async function apiReturns(id: string, image: string): Promise<void> {
    http.expectOne('/api/captcha').flush({ id, image });
    await fixture.whenStable();
  }

  function answerInput(): HTMLInputElement {
    return element.querySelector<HTMLInputElement>('#captchaAnswer')!;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CaptchaField],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    idControl = new FormControl('', { nonNullable: true });
    answerControl = new FormControl('', { nonNullable: true });

    fixture = TestBed.createComponent(CaptchaField);
    element = fixture.nativeElement;
    fixture.componentRef.setInput('idControl', idControl);
    fixture.componentRef.setInput('answerControl', answerControl);
    fixture.detectChanges(); // the component starts and asks for a picture
  });

  afterEach(() => http.verify());

  it('shows the picture from the API and remembers its id', async () => {
    expect(element.textContent).toContain('Loading the picture');

    await apiReturns('first-id', 'data:image/png;base64,AAAA');

    expect(element.querySelector('img')?.getAttribute('src')).toBe('data:image/png;base64,AAAA');
    expect(idControl.value).toBe('first-id');
  });

  it('puts what the visitor types into the answer control', async () => {
    await apiReturns('first-id', 'data:image/png;base64,AAAA');

    answerInput().value = 'ABC123';
    answerInput().dispatchEvent(new Event('input'));

    expect(answerControl.value).toBe('ABC123');
  });

  it('gets another picture and clears the answer after a click on New picture', async () => {
    await apiReturns('first-id', 'data:image/png;base64,AAAA');
    answerControl.setValue('ABC123');

    element.querySelector<HTMLButtonElement>('button')!.click();
    expect(idControl.value).toBe('');
    expect(answerControl.value).toBe('');
    await apiReturns('second-id', 'data:image/png;base64,BBBB');

    expect(element.querySelector('img')?.getAttribute('src')).toBe('data:image/png;base64,BBBB');
    expect(idControl.value).toBe('second-id');
  });

  it('says so when the picture could not be loaded and lets the visitor try again', async () => {
    http.expectOne('/api/captcha').flush('error', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Could not load the picture');
    expect(element.querySelector('img')).toBeNull();
    expect(idControl.value).toBe('');

    element.querySelector<HTMLButtonElement>('button')!.click();
    await apiReturns('second-id', 'data:image/png;base64,BBBB');

    expect(element.querySelector('[role="alert"]')).toBeNull();
    expect(element.querySelector('img')).not.toBeNull();
  });
});
