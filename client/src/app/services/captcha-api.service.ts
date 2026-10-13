import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CaptchaModel } from '../models/captcha.model';

@Injectable({ providedIn: 'root' })
export class CaptchaApiService {
  private readonly http = inject(HttpClient);

  // Every call makes a new picture on the server
  getCaptcha(): Observable<CaptchaModel> {
    return this.http.get<CaptchaModel>(`${environment.apiUrl}/captcha`);
  }
}
