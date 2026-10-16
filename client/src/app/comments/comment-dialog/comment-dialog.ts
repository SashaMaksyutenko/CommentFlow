import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { CommentDialogService } from '../../services/comment-dialog.service';
import { CommentForm } from '../comment-form/comment-form';

// The window with the comment form over the page. It is shown while CommentDialogService says
// so: "Add a comment" opens it for a new comment, a Reply button opens it for an answer.
@Component({
  imports: [CommentForm],
  selector: 'app-comment-dialog',
  styleUrl: './comment-dialog.css',
  templateUrl: './comment-dialog.html',
})
export class CommentDialog {
  protected readonly dialog = inject(CommentDialogService);

  // The white window itself; exists only while it is open
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  constructor() {
    // The keyboard focus has to be inside the window, otherwise Escape would not reach it.
    // For an answer the form already puts the cursor into the text field, then this does nothing.
    effect(() => {
      const panel = this.panel()?.nativeElement;
      if (panel && !panel.contains(document.activeElement)) {
        panel.focus();
      }
    });
  }
}
