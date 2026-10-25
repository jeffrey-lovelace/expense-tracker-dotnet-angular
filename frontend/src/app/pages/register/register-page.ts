import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthStore } from '../../auth/auth.store';
import { defaultAfterLogin } from '../../auth/return-url';
import { AuthLayout } from '../../components/auth-layout/auth-layout';
import { passwordStrength } from '../../forms/password-strength.validator';
import { applyServerErrors } from '../../forms/server-errors';
import { problemOf } from '../../models/problem-details';

@Component({
  selector: 'app-register-page',
  imports: [
    AuthLayout,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // The same limits as the backend (Identity.Domain), for instant feedback. The API stays the authority.
  protected readonly form = new FormGroup({
    displayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(80)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(254)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(128),
        passwordStrength,
      ],
    }),
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
      .register(this.form.getRawValue())
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => void this.router.navigateByUrl(defaultAfterLogin),
        error: (error: unknown) => this.showError(error),
      });
  }

  // 409: the email is taken, shown on the email field. 400: field errors from the domain rules.
  private showError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      return;
    }

    if (error.status === 409) {
      const email = this.form.controls.email;
      email.setErrors({
        server: problemOf(error).detail ?? 'An account with this email already exists.',
      });
      email.markAsTouched();
    } else if (error.status === 400) {
      const rest = applyServerErrors(this.form, error);
      this.formError.set(rest.length > 0 ? rest.join(' ') : null);
    }
  }
}
