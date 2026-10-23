import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { environment } from '../environments/environment';
import { API_CONFIG } from './api-config';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    {
      provide: API_CONFIG,
      useValue: {
        identityApiUrl: environment.identityApiUrl,
        expensesApiUrl: environment.expensesApiUrl,
      },
    },
    // <mat-icon>name</mat-icon> renders the self-hosted Material Symbols (outlined) font.
    provideAppInitializer(() => {
      inject(MatIconRegistry).setDefaultFontSetClass('material-symbols-outlined');
    }),
  ],
};
