import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthStore } from '../../auth/auth.store';
import { safeReturnUrl } from '../../auth/return-url';
import { AuthLayout } from '../../components/auth-layout/auth-layout';
import { applyServerErrors } from '../../forms/server-errors';
import { problemOf } from '../../models/problem-details';

@Component({
  selector: 'app-login-page',
  imports: [
    AuthLayout,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** From the query string (?returnUrl=...), set by the auth guard. */
  readonly returnUrl = input<string>();

  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected readonly pending = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.pending.set(true);
    this.formError.set(null);
    this.auth
      .login(this.form.getRawValue())
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => void this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
        error: (error: unknown) => this.showError(error),
      });
  }

  // 401 is the generic "Invalid email or password." from the API. 0 and 5xx are shown by the error interceptor.
  private showError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      return;
    }

    if (error.status === 401) {
      this.formError.set(problemOf(error).detail ?? 'Invalid email or password.');
    } else if (error.status === 400) {
      const rest = applyServerErrors(this.form, error);
      this.formError.set(rest.length > 0 ? rest.join(' ') : null);
    }
  }
}
