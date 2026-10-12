import { FormControl } from '@angular/forms';
import { EMAIL_PATTERN, USER_NAME_PATTERN, httpUrl, notBlank } from './comment-validators';

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
