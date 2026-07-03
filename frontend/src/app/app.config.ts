import {
  ApplicationConfig,
  isDevMode,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { unauthorizedInterceptor } from './core/auth/unauthorized.interceptor';
import { withCredentialsInterceptor } from './core/auth/with-credentials.interceptor';
import { TranslocoHttpLoader } from './core/i18n/transloco-loader';

const storedLang = typeof localStorage !== 'undefined' ? localStorage.getItem('lang') : null;

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([withCredentialsInterceptor, unauthorizedInterceptor])),
    provideTransloco({
      config: {
        availableLangs: ['de', 'en'],
        defaultLang: storedLang ?? 'de',
        fallbackLang: 'de',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
  ],
};
