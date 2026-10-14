import { Component, ElementRef, OnDestroy, OnInit, input, viewChild } from '@angular/core';
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

  ngOnInit(): void {
    // When the form clears the control (after sending), the browser field has to be cleared
    // too, otherwise it keeps showing the name of the old file
    this.valueChanges = this.control().valueChanges.subscribe((file) => {
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
