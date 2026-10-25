import { ErrorHandler, Injectable } from '@angular/core';

/**
 * Last stop for errors nothing else handled. HTTP errors are already reported by the error interceptor and the
 * pages; this only records genuine bugs. The single place allowed to use console.error (see eslint.config.js).
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.error('Unexpected error', error);
  }
}
