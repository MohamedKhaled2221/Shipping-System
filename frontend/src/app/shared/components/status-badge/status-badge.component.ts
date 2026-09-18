import { Component, input } from '@angular/core';

/**
 * A single generic badge that colors itself by "tone" rather than re-implementing status
 * logic per enum. Feature code maps its own enum value + label map to a tone via
 * shared/status-tone.ts, then just passes label + tone in here.
 */
export type StatusTone = 'neutral' | 'progress' | 'success' | 'warning' | 'danger';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="badge" [class]="'badge--' + tone()">{{ label() }}</span>`,
  styles: [
    `
      .badge {
        display: inline-block;
        font-family: var(--font-display);
        font-size: 0.72rem;
        font-weight: 600;
        letter-spacing: 0.02em;
        padding: 0.25em 0.65em;
        border-radius: 999px;
        border: 1px solid transparent;
        white-space: nowrap;
      }
      .badge--neutral { background: #eceae3; color: var(--color-muted); }
      .badge--progress { background: #fdeed9; color: var(--color-amber-dark); border-color: #f3cf9a; }
      .badge--success { background: #e2efee; color: var(--color-teal); border-color: #bcdad8; }
      .badge--warning { background: #fdeed9; color: var(--color-amber-dark); border-color: #f3cf9a; }
      .badge--danger { background: #f6e3e0; color: var(--color-red); border-color: #eabdb6; }
    `,
  ],
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');
}
