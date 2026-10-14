import { Component, input } from '@angular/core';
import { AttachmentModel } from '../../models/comment.model';

// Shows the file of a comment: a small picture, a link to a TXT file,
// or a short note while the server is still working on an image
@Component({
  selector: 'app-attachment-view',
  styleUrl: './attachment-view.css',
  templateUrl: './attachment-view.html',
})
export class AttachmentView {
  readonly attachment = input.required<AttachmentModel>();
}
