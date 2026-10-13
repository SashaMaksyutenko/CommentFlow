import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CaptchaModel } from '../models/captcha.model';
import { CaptchaApiService } from './captcha-api.service';

describe('CaptchaApiService', () => {
  let service: CaptchaApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CaptchaApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('asks the API for a picture and returns it as it is', () => {
    const captcha: CaptchaModel = { id: 'abc123', image: 'data:image/png;base64,AAAA' };
    let result: CaptchaModel | undefined;

    service.getCaptcha().subscribe((r) => (result = r));
    const request = http.expectOne('/api/captcha');
    request.flush(captcha);

    expect(request.request.method).toBe('GET');
    expect(result).toEqual(captcha);
  });
});
