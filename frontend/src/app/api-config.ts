import { InjectionToken } from '@angular/core';

/** Base URLs of the two APIs. Services inject this instead of importing `environment`, so tests can swap it. */
export interface ApiConfig {
  readonly identityApiUrl: string;
  readonly expensesApiUrl: string;
}

export const API_CONFIG = new InjectionToken<ApiConfig>('API_CONFIG');
