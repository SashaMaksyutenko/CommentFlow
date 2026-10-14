import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AttachmentModel } from '../../models/comment.model';
import { AttachmentView } from './attachment-view';

describe('AttachmentView', () => {
  let fixture: ComponentFixture<AttachmentView>;
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

  async function show(file: AttachmentModel): Promise<void> {
    fixture.componentRef.setInput('attachment', file);
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AttachmentView] }).compileComponents();

    fixture = TestBed.createComponent(AttachmentView);
    element = fixture.nativeElement;
  });

  it('shows a ready image as a small picture that opens the file', async () => {
    await show(attachment());

    const picture = element.querySelector('a.image img')!;
    expect(picture.getAttribute('src')).toBe('/uploads/abc.png');
    expect(picture.getAttribute('alt')).toBe('photo.png');
    expect(element.querySelector('a.image')?.getAttribute('href')).toBe('/uploads/abc.png');
  });

  it('shows a ready text file as a link with its name', async () => {
    await show(attachment({ url: '/uploads/def.txt', fileName: 'notes.txt', type: 'Text', contentType: 'text/plain' }));

    const link = element.querySelector('a.text-file')!;
    expect(link.textContent).toBe('notes.txt');
    expect(link.getAttribute('href')).toBe('/uploads/def.txt');
    expect(element.querySelector('img')).toBeNull();
  });

  it('says the image is being processed and does not try to load it yet', async () => {
    await show(attachment({ status: 'Processing' }));

    expect(element.textContent).toContain('is being processed');
    expect(element.querySelector('img')).toBeNull();
    expect(element.querySelector('a')).toBeNull();
  });

  it('says when the image could not be processed', async () => {
    await show(attachment({ status: 'Failed' }));

    expect(element.querySelector('.failed')?.textContent).toContain('could not be processed');
    expect(element.querySelector('img')).toBeNull();
  });

  it('shows a file name with HTML in it as plain text', async () => {
    await show(attachment({ type: 'Text', fileName: '<b>bold</b>.txt' }));

    expect(element.querySelector('a.text-file')?.textContent).toBe('<b>bold</b>.txt');
    expect(element.querySelector('b')).toBeNull();
  });
});
