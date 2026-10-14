import { Component, ElementRef, OnDestroy, OnInit, input, signal, viewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription } from 'rxjs';

// The file part of the comment form. A file can't be bound with formControlName
// the way a text field is, so the chosen file is put into the control by hand.
@Component({
  selector: 'app-attachment-field',
  styleUrl: './attachment-field.css',
  templateUrl: './attachment-field.html',
})
export class AttachmentField implements OnInit, OnDestroy {
  // The control of the form that holds the chosen file, null when there is none
  readonly control = input.required<FormControl<File | null>>();

  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');
  private valueChanges?: Subscription;

  // The chosen file for the template. The value of a form control is not a signal,
  // so without this the field would not redraw when the form clears the control after sending.
  protected readonly file = signal<File | null>(null);

  ngOnInit(): void {
    this.file.set(this.control().value);

    this.valueChanges = this.control().valueChanges.subscribe((file) => {
      this.file.set(file);

      // The browser field has to be cleared too, otherwise it keeps showing the old file name
      if (file === null) {
        this.fileInput().nativeElement.value = '';
      }
    });
  }

  ngOnDestroy(): void {
    this.valueChanges?.unsubscribe();
  }

  protected choose(files: FileList | null): void {
    this.control().setValue(files?.[0] ?? null);
    // Show a wrong type or size right away, not only after Add comment
    this.control().markAsTouched();
  }

  protected remove(): void {
    this.control().setValue(null);
  }
}
