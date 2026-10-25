import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Split layout of the login and register pages (AGENTS.md 12.4): an ink panel with one serif statement on the
 * left, the form on the right. Stacked on small screens. Presentational: the page projects its form into it.
 */
@Component({
  selector: 'app-auth-layout',
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthLayout {}
