import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShipmentDto } from '../../../core/models/shipment.models';
import { ShipmentService } from '../../../core/services/shipment.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import {
  shipmentStatusLabel,
  shipmentStatusTone,
} from '../../../shared/status-tone';

@Component({
  selector: 'app-agent-shipment-list',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    RouterLink,
    StatusBadgeComponent,
  ],
  template: `
    <h1>My deliveries</h1>

    @if (loading()) {
      <p class="hint-text">Loading…</p>
    } @else if (shipments().length === 0) {
      <div class="empty-state">
        Nothing assigned to you right now.
      </div>
    } @else {
      <table class="manifest-table">
        <thead>
          <tr>
            <th>Tracking #</th>
            <th>Status</th>
            <th>Est. delivery</th>
            <th></th>
          </tr>
        </thead>

        <tbody>
          @for (s of shipments(); track s.id) {
            <tr>
              <td class="mono">
                {{ s.trackingNumber }}
              </td>

              <td>
                <app-status-badge
                  [label]="shipmentStatusLabel(s.status)"
                  [tone]="shipmentStatusTone(s.status)"
                />
              </td>

              <td>
                {{
                  s.estimatedDeliveryDate
                    ? (s.estimatedDeliveryDate | date: 'mediumDate')
                    : '—'
                }}
              </td>

              <td>
                <a
                  [routerLink]="['/agent/shipments', s.id]"
                  class="btn-outline btn-sm"
                >
                  Open
                </a>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,

  styles: [
    `
      .mono {
        font-family: var(--font-mono);
        font-size: 0.85rem;
      }
    `,
  ],
})
export class AgentShipmentListComponent implements OnInit {
  private readonly shipmentService = inject(ShipmentService);

  readonly shipments = signal<ShipmentDto[]>([]);
  readonly loading = signal(true);

  readonly shipmentStatusLabel = shipmentStatusLabel;
  readonly shipmentStatusTone = shipmentStatusTone;

  ngOnInit(): void {
    this.shipmentService.getMyAssignedShipments().subscribe({
      next: (shipments) => {
        this.shipments.set(shipments);
        this.loading.set(false);
      },

      error: () => {
        this.loading.set(false);
      },
    });
  }
}