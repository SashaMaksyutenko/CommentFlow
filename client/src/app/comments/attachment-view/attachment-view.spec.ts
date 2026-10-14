import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AttachmentModel } from '../../models/comment.model';
import { AttachmentView } from './attachment-view';

describe('AttachmentView', () => {
  let fixture: ComponentFixture<AttachmentView>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // A ready image; a test changes only the fields it cares about
  function attachment(changes: Partial<AttachmentModel> = {}): AttachmentModel {
    return {
      url: '/uploads/abc.png',
      fileName: 'photo.png',
      type: 'Image',
      status: 'Ready',
      contentType: 'image/png',
      size: 41165,
      ...changes,
    };
  }

  const textFile = attachment({ url: '/uploads/def.txt', fileName: 'notes.txt', type: 'Text', contentType: 'text/plain' });

  async function show(file: AttachmentModel): Promise<void> {
    fixture.componentRef.setInput('attachment', file);
    await fixture.whenStable();
  }

  function lightbox(): HTMLElement | null {
    return element.querySelector('.lightbox');
  }

  // A click on the small picture or on the TXT file name
  async function openLightbox(): Promise<void> {
    element.querySelector<HTMLButtonElement>('.thumbnail, .text-file')!.click();
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AttachmentView],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AttachmentView);
    element = fixture.nativeElement;
  });

  afterEach(() => http.verify());

  it('shows a ready image as a small picture', async () => {
    await show(attachment());

    const picture = element.querySelector('.thumbnail img')!;
    expect(picture.getAttribute('src')).toBe('/uploads/abc.png');
    expect(picture.getAttribute('alt')).toBe('photo.png');
    expect(lightbox()).toBeNull();
  });

  it('shows a ready text file by its name', async () => {
    await show(textFile);

    expect(element.querySelector('.text-file')?.textContent).toBe('notes.txt');
    expect(element.querySelector('img')).toBeNull();
  });

  it('says the image is being processed and does not try to load it yet', async () => {
    await show(attachment({ status: 'Processing' }));

    expect(element.textContent).toContain('is being processed');
    expect(element.querySelector('img')).toBeNull();
    expect(element.querySelector('button')).toBeNull();
  });

  it('says when the image could not be processed', async () => {
    await show(attachment({ status: 'Failed' }));

    expect(element.querySelector('.failed')?.textContent).toContain('could not be processed');
    expect(element.querySelector('button')).toBeNull();
  });

  it('shows a file name with HTML in it as plain text', async () => {
    await show(attachment({ type: 'Text', fileName: '<b>bold</b>.txt' }));

    expect(element.querySelector('.text-file')?.textContent).toBe('<b>bold</b>.txt');
    expect(element.querySelector('b')).toBeNull();
  });

  it('opens the picture over the page and gives the focus to the close button', async () => {
    await show(attachment());

    await openLightbox();

    expect(lightbox()?.querySelector('.content img')?.getAttribute('src')).toBe('/uploads/abc.png');
    expect(lightbox()?.getAttribute('role')).toBe('dialog');
    expect(document.activeElement).toBe(element.querySelector('.close'));
  });

  it('closes with the close button, with Escape and with a click on the dark area', async () => {
    await show(attachment());

    await openLightbox();
    element.querySelector<HTMLButtonElement>('.close')!.click();
    await fixture.whenStable();
    expect(lightbox()).toBeNull();

    await openLightbox();
    element.querySelector('.close')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();
    expect(lightbox()).toBeNull();

    await openLightbox();
    lightbox()!.click();
    await fixture.whenStable();
    expect(lightbox()).toBeNull();
  });

  it('stays open after a click on the file itself', async () => {
    await show(attachment());
    await openLightbox();

    element.querySelector<HTMLElement>('.content img')!.click();
    await fixture.whenStable();

    expect(lightbox()).not.toBeNull();
  });

  it('can hold the focus itself, so Escape still works after a click on the picture', async () => {
    await show(attachment());
    await openLightbox();

    // In a browser a click on the picture moves the focus to the nearest element that can take it
    lightbox()!.focus();
    expect(document.activeElement).toBe(lightbox());
    lightbox()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(lightbox()).toBeNull();
  });

  it('loads the text of a TXT file once and shows it in the lightbox', async () => {
    await show(textFile);

    await openLightbox();
    expect(lightbox()?.textContent).toContain('Loading');
    http.expectOne('/uploads/def.txt').flush('Hello\n<b>not a tag</b>');
    await fixture.whenStable();

    expect(lightbox()?.querySelector('pre')?.textContent).toBe('Hello\n<b>not a tag</b>');
    expect(lightbox()?.querySelector('b')).toBeNull();

    // Opened again: the text is already there, no second request (afterEach checks it)
    element.querySelector<HTMLButtonElement>('.close')!.click();
    await openLightbox();
    expect(lightbox()?.querySelector('pre')).not.toBeNull();
  });

  it('says so when the text file could not be loaded', async () => {
    await show(textFile);

    await openLightbox();
    http.expectOne('/uploads/def.txt').flush('missing', { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(lightbox()?.querySelector('.failed')?.textContent).toContain('Could not load the file');
  });
});
