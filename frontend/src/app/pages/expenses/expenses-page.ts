import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthStore } from '../../auth/auth.store';
import { AppShell } from '../../components/app-shell/app-shell';

/** The signed-in home. The ledger itself (list, filters, totals, dialogs) arrives in F3. */
@Component({
  selector: 'app-expenses-page',
  imports: [AppShell],
  templateUrl: './expenses-page.html',
  styleUrl: './expenses-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpensesPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly userName = computed(() => this.auth.user()?.displayName ?? '');

  protected signOut(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
