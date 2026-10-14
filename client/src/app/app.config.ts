import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { CommentsHubService } from './services/comments-hub.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    // Live updates start together with the app. Nothing is returned here,
    // so the page does not wait for the connection and shows up at once.
    provideAppInitializer(() => {
      inject(CommentsHubService).connect();
    }),
  ]
};
