import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

/** Header and content frame for signed-in pages. Presentational: the page passes the name and handles sign-out. */
@Component({
  selector: 'app-shell',
  imports: [MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  readonly userName = input.required<string>();
  readonly signOut = output();
}
