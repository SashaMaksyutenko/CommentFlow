import { Component, ElementRef, OnDestroy, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AttachmentModel } from '../../models/comment.model';
import { CommentsApiService } from '../../services/comments-api.service';

// Shows the file of a comment: a small picture, a TXT file name,
// or a short note while the server is still working on an image.
// A click opens the file in a lightbox: a dark layer over the page with the file in the middle.
@Component({
  selector: 'app-attachment-view',
  styleUrl: './attachment-view.css',
  templateUrl: './attachment-view.html',
})
export class AttachmentView implements OnDestroy {
  private readonly api = inject(CommentsApiService);
  private request?: Subscription;

  readonly attachment = input.required<AttachmentModel>();

  // The API gives "/uploads/<name>". When the page and the API are on different sites (Vercel and Render),
  // the address of the API is put in front, or the browser would look for the file on the page's site.
  protected readonly fileUrl = computed(() => environment.filesUrl + this.attachment().url);

  protected readonly isOpen = signal(false);

  // The big picture fades in only when it has arrived, so it is never seen half drawn
  protected readonly imageLoaded = signal(false);

  // The content of a TXT file, loaded the first time the lightbox is opened
  protected readonly text = signal<string | null>(null);
  protected readonly textFailed = signal(false);

  // Exists only while the lightbox is open
  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');

  constructor() {
    // When the lightbox appears, the keyboard focus goes to its close button,
    // so Escape works at once and Enter closes it
    effect(() => {
      this.closeButton()?.nativeElement.focus();
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }

  protected open(): void {
    // The picture element is created again with every opening, so it fades in again too
    this.imageLoaded.set(false);
    this.isOpen.set(true);

    if (this.attachment().type === 'Text' && this.text() === null) {
      this.loadText();
    }
  }

  protected close(): void {
    this.isOpen.set(false);
  }

  private loadText(): void {
    this.textFailed.set(false);

    this.request?.unsubscribe();
    this.request = this.api.getAttachmentText(this.fileUrl()).subscribe({
      next: (text) => this.text.set(text),
      error: () => this.textFailed.set(true),
    });
  }
}
