import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A placeholder block with a shimmer, sized like the content it stands in for, so nothing shifts when data
 * arrives (AGENTS.md 12.5). Static under prefers-reduced-motion. Hidden from screen readers; the page announces
 * loading with aria-busy instead.
 */
@Component({
  selector: 'app-skeleton',
  templateUrl: './skeleton.html',
  styleUrl: './skeleton.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
    '[class]': "'skeleton ' + variant()",
    '[style.width]': 'width()',
    '[style.height]': 'height()',
  },
})
export class Skeleton {
  readonly variant = input<'line' | 'block' | 'circle'>('line');
  readonly width = input('100%');
  readonly height = input<string | undefined>(undefined);
}
