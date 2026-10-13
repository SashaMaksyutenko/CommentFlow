import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TagToolbar } from './tag-toolbar';

// The toolbar needs a real text field next to it, so the tests put both on a small page
@Component({
  imports: [ReactiveFormsModule, TagToolbar],
  template: `
    <app-tag-toolbar [field]="field" [control]="control" />
    <textarea #field [formControl]="control"></textarea>
  `,
})
class TestPage {
  readonly control = new FormControl('', { nonNullable: true });
}

describe('TagToolbar', () => {
  let fixture: ComponentFixture<TestPage>;
  let element: HTMLElement;
  let field: HTMLTextAreaElement;

  // Puts the text into the field and selects a part of it (start = end means just a cursor)
  function typeAndSelect(text: string, start: number, end: number): void {
    fixture.componentInstance.control.setValue(text);
    field.setSelectionRange(start, end);
  }

  function button(label: string): HTMLButtonElement {
    const buttons = Array.from(element.querySelectorAll('button'));
    return buttons.find((item) => item.textContent?.trim() === label)!;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TestPage] }).compileComponents();

    fixture = TestBed.createComponent(TestPage);
    element = fixture.nativeElement;
    fixture.detectChanges();
    field = element.querySelector('textarea')!;
  });

  it('has a button for every allowed tag', () => {
    const labels = Array.from(element.querySelectorAll('button')).map((item) => item.textContent?.trim());

    expect(labels).toEqual(['[i]', '[strong]', '[code]', '[a]']);
  });

  it('puts the tag around the selected text and the cursor after it', () => {
    typeAndSelect('Hello world', 6, 11);

    button('[strong]').click();

    expect(field.value).toBe('Hello <strong>world</strong>');
    expect(field.selectionStart).toBe('Hello <strong>world</strong>'.length);
    expect(field.selectionEnd).toBe(field.selectionStart);
  });

  it.each(['i', 'strong', 'code'])('puts an empty <%s> at the cursor and the cursor inside it', (tag) => {
    typeAndSelect('Hello  world', 6, 6);

    button(`[${tag}]`).click();

    expect(field.value).toBe(`Hello <${tag}></${tag}> world`);
    expect(field.selectionStart).toBe(`Hello <${tag}>`.length);
  });

  it('makes a link of the selected text and puts the cursor into href', () => {
    typeAndSelect('See docs', 4, 8);

    button('[a]').click();

    expect(field.value).toBe('See <a href="" title="">docs</a>');
    expect(field.selectionStart).toBe('See <a href="'.length);
  });

  it('gives the cursor back to the text field and marks the text as changed', () => {
    typeAndSelect('Hello', 5, 5);

    button('[code]').click();

    expect(document.activeElement).toBe(field);
    expect(fixture.componentInstance.control.dirty).toBe(true);
  });

  it('does not take the cursor away from the text field when a button is pressed', () => {
    const press = new MouseEvent('mousedown', { cancelable: true });

    button('[i]').dispatchEvent(press);

    expect(press.defaultPrevented).toBe(true);
  });
});
