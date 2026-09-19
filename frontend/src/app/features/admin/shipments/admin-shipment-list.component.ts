import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ShipmentDto } from '../../../core/models/shipment.models';
import { RECENT_SHIPMENTS_NAMESPACE, RecentLookupService } from '../../../core/services/recent-lookup.service';
import { ShipmentService } from '../../../core/services/shipment.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { shipmentStatusLabel, shipmentStatusTone } from '../../../shared/status-tone';

@Component({
  selector: 'app-admin-shipment-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, StatusBadgeComponent],
  template: `
    <div class="header-row">
      <h1>Shipments</h1>
      <form [formGroup]="lookupForm" (ngSubmit)="lookup()" class="lookup-form">
        <input formControlName="shipmentId" placeholder="Open a shipment by ID" />
        <button type="submit" class="btn-outline btn-sm" [disabled]="lookupForm.invalid">Open</button>
      </form>
    </div>

    <p class="hint-text">
      The API doesn't expose a paged "list all shipments" endpoint yet — this shows shipments
      opened or assigned from this browser. Add <code>GET /api/v1/shipments</code>
      (filterable by status/agent) to the backend for a real dashboard, then swap this list for
      that call.
    </p>

    @if (loading()) {
      <p class="hint-text">Loading…</p>
    } @else if (shipments().length === 0) {
      <div class="empty-state">No shipments opened yet — paste a shipment ID above to get started.</div>
    } @else {
      <table class="manifest-table">
        <thead>
          <tr>
            <th>Tracking #</th>
            <th>Status</th>
            <th>Agent</th>
            <th>Fee</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (s of shipments(); track s.id) {
            <tr>
              <td class="mono">{{ s.trackingNumber }}</td>
              <td><app-status-badge [label]="shipmentStatusLabel(s.status)" [tone]="shipmentStatusTone(s.status)" /></td>
              <td class="mono">{{ s.deliveryAgentId ? s.deliveryAgentId.slice(0, 8) + '…' : 'Unassigned' }}</td>
              <td>{{ s.shippingFee | number: '1.2-2' }}</td>
              <td><a [routerLink]="['/admin/shipments', s.id]" class="btn-outline btn-sm">Open</a></td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: [
    `
      .header-row { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; }
      .lookup-form { display: flex; gap: 0.5rem; }
      .lookup-form input { width: 280px; }
      .mono { font-family: var(--font-mono); font-size: 0.82rem; }
    `,
  ],
})
export class AdminShipmentListComponent implements OnInit {
  private readonly shipmentService = inject(ShipmentService);
  private readonly recent = inject(RecentLookupService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly shipments = signal<ShipmentDto[]>([]);
  readonly loading = signal(true);

  readonly shipmentStatusLabel = shipmentStatusLabel;
  readonly shipmentStatusTone = shipmentStatusTone;

  readonly lookupForm = this.fb.nonNullable.group({
    shipmentId: ['', Validators.required],
  });

  ngOnInit(): void {
    const ids = this.recent.list(RECENT_SHIPMENTS_NAMESPACE);
    if (ids.length === 0) {
      this.loading.set(false);
      return;
    }
    forkJoin(ids.map((id) => this.shipmentService.getById(id).pipe(catchError(() => of(null))))).subscribe((results) => {
      this.shipments.set(results.filter((s): s is ShipmentDto => s !== null));
      this.loading.set(false);
    });
  }

  lookup(): void {
    if (this.lookupForm.invalid) return;
    this.router.navigate(['/admin/shipments', this.lookupForm.getRawValue().shipmentId]);
  }
}
