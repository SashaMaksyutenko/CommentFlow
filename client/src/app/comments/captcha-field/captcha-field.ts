import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { CaptchaApiService } from '../../services/captcha-api.service';

// The CAPTCHA part of a form: the picture, a button for another picture and the field for the answer
@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-captcha-field',
  styleUrl: './captcha-field.css',
  templateUrl: './captcha-field.html',
})
export class CaptchaField implements OnInit, OnDestroy {
  private readonly api = inject(CaptchaApiService);
  private request?: Subscription;

  // Two controls of the form this field is in: the id of the picture and what the visitor typed
  readonly idControl = input.required<FormControl<string>>();
  readonly answerControl = input.required<FormControl<string>>();

  // The picture as a data URL, null while there is none
  protected readonly image = signal<string | null>(null);
  protected readonly loadFailed = signal(false);

  ngOnInit(): void {
    this.reload();
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }

  // Gets a new picture. The old one stops working, so its id and the typed answer are thrown away.
  reload(): void {
    this.loadFailed.set(false);
    this.idControl().setValue('');
    this.answerControl().reset();

    this.request?.unsubscribe();
    this.request = this.api.getCaptcha().subscribe({
      next: (captcha) => {
        this.idControl().setValue(captcha.id);
        this.image.set(captcha.image);
      },
      error: () => {
        this.image.set(null);
        this.loadFailed.set(true);
      },
    });
  }
}
