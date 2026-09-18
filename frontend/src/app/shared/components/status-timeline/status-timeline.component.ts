import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { ShipmentStatusHistoryDto } from '../../../core/models/shipment.models';
import { shipmentStatusLabel, shipmentStatusTone } from '../../status-tone';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

/**
 * Renders a shipment's status history (FR-6.5/FR-9.2) as a vertical timeline. Numbered
 * markers are appropriate here — unlike most UI lists, this content genuinely is a
 * chronological sequence the state machine enforces.
 */
@Component({
  selector: 'app-status-timeline',
  standalone: true,
  imports: [CommonModule, StatusBadgeComponent],
  template: `
    <ol class="timeline">
      @for (entry of history(); track entry.changedAt + entry.status; let i = $index) {
        <li class="timeline__item" [class.timeline__item--current]="i === history().length - 1">
          <span class="timeline__marker">{{ i + 1 }}</span>
          <div class="timeline__body">
            <div class="timeline__head">
              <app-status-badge [label]="shipmentStatusLabel(entry.status)" [tone]="shipmentStatusTone(entry.status)" />
              <time class="timeline__time">{{ entry.changedAt | date: 'medium' }}</time>
            </div>
            <div class="timeline__meta">by {{ entry.changedBy }}</div>
            @if (entry.notes) {
              <div class="timeline__notes">{{ entry.notes }}</div>
            }
          </div>
        </li>
      } @empty {
        <li class="empty-state">No status history yet.</li>
      }
    </ol>
  `,
  styles: [
    `
      .timeline { list-style: none; margin: 0; padding: 0; }
      .timeline__item {
        display: flex;
        gap: 0.85rem;
        padding-bottom: 1.1rem;
        position: relative;
      }
      .timeline__item:not(:last-child)::before {
        content: '';
        position: absolute;
        left: 12px;
        top: 26px;
        bottom: 0;
        width: 1px;
        background: var(--color-line);
      }
      .timeline__marker {
        flex: none;
        width: 25px;
        height: 25px;
        border-radius: 50%;
        background: var(--color-ink);
        color: #fff;
        font-family: var(--font-mono);
        font-size: 0.72rem;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .timeline__item--current .timeline__marker { background: var(--color-amber); }
      .timeline__body { flex: 1; padding-top: 2px; }
      .timeline__head { display: flex; align-items: center; gap: 0.6rem; }
      .timeline__time { font-family: var(--font-mono); font-size: 0.76rem; color: var(--color-muted); }
      .timeline__meta { font-size: 0.78rem; color: var(--color-muted); margin-top: 0.25rem; }
      .timeline__notes { font-size: 0.85rem; margin-top: 0.35rem; }
    `,
  ],
})
export class StatusTimelineComponent {
  readonly history = input.required<ShipmentStatusHistoryDto[]>();

  readonly shipmentStatusLabel = shipmentStatusLabel;
  readonly shipmentStatusTone = shipmentStatusTone;
}
