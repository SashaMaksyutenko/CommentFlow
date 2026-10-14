import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { AttachmentField } from './attachment-field';

describe('AttachmentField', () => {
  let fixture: ComponentFixture<AttachmentField>;
  let element: HTMLElement;
  let control: FormControl<File | null>;
  let fileInput: HTMLInputElement;

  const photo = new File(['picture'], 'photo.png', { type: 'image/png' });

  // A test can't put a file into a file input the way a visitor does,
  // so the list of chosen files is set by hand before the change event
  async function choose(files: File[]): Promise<void> {
    Object.defineProperty(fileInput, 'files', { value: files, configurable: true });
    fileInput.dispatchEvent(new Event('change'));
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AttachmentField] }).compileComponents();

    control = new FormControl<File | null>(null);
    fixture = TestBed.createComponent(AttachmentField);
    element = fixture.nativeElement;
    fixture.componentRef.setInput('control', control);
    fixture.detectChanges();
    fileInput = element.querySelector('input[type="file"]')!;
  });

  it('lets the visitor choose only the allowed kinds of files and says what is allowed', () => {
    expect(fileInput.accept).toBe('.jpg,.jpeg,.png,.gif,.txt');
    expect(element.querySelector('.hint')?.textContent).toContain('up to 100 KB');
  });

  it('puts the chosen file into the control', async () => {
    await choose([photo]);

    expect(control.value).toBe(photo);
    expect(control.touched).toBe(true);
    expect(element.querySelector('.remove')?.textContent).toContain('Remove photo.png');
  });

  it('leaves the control empty when the visitor closes the dialog without a file', async () => {
    await choose([]);

    expect(control.value).toBeNull();
    expect(element.querySelector('.remove')).toBeNull();
  });

  it('forgets the file after a click on Remove', async () => {
    await choose([photo]);

    element.querySelector<HTMLButtonElement>('.remove')!.click();
    await fixture.whenStable();

    expect(control.value).toBeNull();
    expect(fileInput.value).toBe('');
    expect(element.querySelector('.remove')).toBeNull();
  });

  it('clears the browser field when the form clears the control', async () => {
    await choose([photo]);
    // What the browser shows after a file is chosen
    Object.defineProperty(fileInput, 'value', { value: 'C:\\fakepath\\photo.png', writable: true });

    control.reset();

    expect(fileInput.value).toBe('');
  });
});
