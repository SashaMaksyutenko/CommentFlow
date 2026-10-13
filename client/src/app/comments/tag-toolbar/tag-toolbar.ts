import { Component, input } from '@angular/core';
import { FormControl } from '@angular/forms';

// The tags a comment may have, the same list as on the server
type Tag = 'i' | 'strong' | 'code' | 'a';

// Buttons that put a tag into the comment text, around the selected part or at the cursor
@Component({
  selector: 'app-tag-toolbar',
  styleUrl: './tag-toolbar.css',
  templateUrl: './tag-toolbar.html',
})
export class TagToolbar {
  // The text field the buttons work with and the form control behind it
  readonly field = input.required<HTMLTextAreaElement>();
  readonly control = input.required<FormControl<string>>();

  protected readonly buttons: { tag: Tag; title: string }[] = [
    { tag: 'i', title: 'Italic' },
    { tag: 'strong', title: 'Bold' },
    { tag: 'code', title: 'Code' },
    { tag: 'a', title: 'Link' },
  ];

  protected insert(tag: Tag): void {
    const field = this.field();
    const text = this.control().value;

    // The selected part of the text. When nothing is selected, start and end are the cursor.
    const start = field.selectionStart;
    const end = field.selectionEnd;
    const selected = text.slice(start, end);

    const opening = tag === 'a' ? '<a href="" title="">' : `<${tag}>`;
    const closing = `</${tag}>`;

    this.control().setValue(text.slice(0, start) + opening + selected + closing + text.slice(end));
    this.control().markAsDirty();

    // Where the visitor types next
    let cursor: number;
    if (tag === 'a') {
      // Between the quotes of href, the address goes there
      cursor = start + '<a href="'.length;
    } else if (selected === '') {
      // Inside the empty tag
      cursor = start + opening.length;
    } else {
      // After the tag that now holds the selected text
      cursor = start + opening.length + selected.length + closing.length;
    }

    field.focus();
    field.setSelectionRange(cursor, cursor);
  }
}
