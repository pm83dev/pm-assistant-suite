import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { AuthInterceptor } from './auth/auth.interceptor';
import { ChatInterceptor } from './auth/chat.interceptor';
import { initPageReload } from './app.init';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([AuthInterceptor, ChatInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: initPageReload,
      multi: true,
    },
  ]
};
