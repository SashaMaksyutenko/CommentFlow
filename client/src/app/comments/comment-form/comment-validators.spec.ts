import { FormControl } from '@angular/forms';
import {
  EMAIL_PATTERN,
  MAX_IMAGE_SIZE,
  MAX_TEXT_FILE_SIZE,
  USER_NAME_PATTERN,
  allowedFile,
  httpUrl,
  notBlank,
} from './comment-validators';

describe('comment validators', () => {
  // A form control with the given text, the way a validator gets it from the form
  function control(value: string): FormControl<string> {
    return new FormControl(value, { nonNullable: true });
  }

  describe('user name pattern', () => {
    it.each(['Sasha1', 'ABC', '2026'])('accepts "%s"', (value) => {
      expect(USER_NAME_PATTERN.test(value)).toBe(true);
    });

    it.each(['Саша', 'Sasha 1', 'sasha_1', 'sasha-1', '<b>Sasha</b>', ''])('rejects "%s"', (value) => {
      expect(USER_NAME_PATTERN.test(value)).toBe(false);
    });
  });

  describe('e-mail pattern', () => {
    it.each(['sasha@example.com', 'first.last+tag@mail.example.org'])('accepts "%s"', (value) => {
      expect(EMAIL_PATTERN.test(value)).toBe(true);
    });

    it.each(['sasha', 'sasha@', '@example.com', 'sasha@example', 'sasha@example.c', 'sa sha@example.com'])(
      'rejects "%s"',
      (value) => {
        expect(EMAIL_PATTERN.test(value)).toBe(false);
      },
    );
  });

  describe('notBlank', () => {
    it.each(['', '   ', '\n\t'])('says the value %j is missing', (value) => {
      expect(notBlank(control(value))).toEqual({ required: true });
    });

    it('accepts a value with some text', () => {
      expect(notBlank(control(' Hi '))).toBeNull();
    });
  });

  describe('allowedFile', () => {
    // A file with the given name and size in bytes, the content does not matter here
    function file(name: string, size: number): FormControl<File | null> {
      return new FormControl<File | null>(new File([new Uint8Array(size)], name));
    }

    it('accepts no file, the field is optional', () => {
      expect(allowedFile(new FormControl<File | null>(null))).toBeNull();
    });

    it.each(['photo.jpg', 'photo.JPEG', 'photo.png', 'anim.gif', 'notes.txt'])('accepts "%s"', (name) => {
      expect(allowedFile(file(name, 10))).toBeNull();
    });

    it.each(['virus.exe', 'photo.bmp', 'page.html', 'photo.png.exe', 'no-extension'])('refuses "%s"', (name) => {
      expect(allowedFile(file(name, 10))).toEqual({ fileType: true });
    });

    it('accepts a TXT file of exactly 100 KB and refuses a bigger one', () => {
      expect(allowedFile(file('notes.txt', MAX_TEXT_FILE_SIZE))).toBeNull();
      expect(allowedFile(file('notes.txt', MAX_TEXT_FILE_SIZE + 1))).toEqual({ textFileTooBig: true });
    });

    it('accepts an image of exactly 5 MB and refuses a bigger one', () => {
      expect(allowedFile(file('photo.png', MAX_IMAGE_SIZE))).toBeNull();
      expect(allowedFile(file('photo.png', MAX_IMAGE_SIZE + 1))).toEqual({ imageTooBig: true });
    });
  });

  describe('httpUrl', () => {
    it.each(['', '   '])('accepts the empty value %j', (value) => {
      expect(httpUrl(control(value))).toBeNull();
    });

    it.each(['http://example.com', 'https://example.com/path?x=1', ' https://example.com '])('accepts "%s"', (value) => {
      expect(httpUrl(control(value))).toBeNull();
    });

    it.each(['example.com', 'ftp://example.com', 'javascript:alert(1)', 'http://', 'just text'])(
      'rejects "%s"',
      (value) => {
        expect(httpUrl(control(value))).toEqual({ httpUrl: true });
      },
    );
  });
});
