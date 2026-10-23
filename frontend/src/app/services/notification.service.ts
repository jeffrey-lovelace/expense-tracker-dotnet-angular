import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Short messages at the bottom of the screen: success ("Expense saved") and network or server errors. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  info(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000 });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 6000, politeness: 'assertive' });
  }
}
