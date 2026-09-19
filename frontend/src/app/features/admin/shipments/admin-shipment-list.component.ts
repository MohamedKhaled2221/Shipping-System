import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ShipmentStatus } from '../../../core/models/enums';
import { PagedResult } from '../../../core/models/product.models';
import { ShipmentDto } from '../../../core/models/shipment.models';
import { ShipmentService } from '../../../core/services/shipment.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { shipmentStatusLabel, shipmentStatusTone } from '../../../shared/status-tone';

@Component({
  selector: 'app-admin-shipment-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, StatusBadgeComponent, PaginationComponent],
  template: `
    <div class="header-row">
      <h1>Shipments</h1>
      <form [formGroup]="lookupForm" (ngSubmit)="lookup()" class="lookup-form">
        <input formControlName="shipmentId" placeholder="Open a shipment by ID" />
        <button type="submit" class="btn-outline btn-sm" [disabled]="lookupForm.invalid">Open</button>
      </form>
    </div>

    <div class="filter-row">
      <label>
        Status
        <select [formControl]="statusFilter" (change)="load(1)">
          <option [ngValue]="null">All statuses</option>
          @for (status of statusOptions; track status) {
            <option [ngValue]="status">{{ shipmentStatusLabel(status) }}</option>
          }
        </select>
      </label>
    </div>

    @if (loading()) {
      <p class="hint-text">Loading…</p>
    } @else if (page()?.items?.length === 0) {
      <div class="empty-state">No shipments match this filter.</div>
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
          @for (s of page()?.items; track s.id) {
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

      @if (page(); as p) {
        <app-pagination [pageNumber]="p.pageNumber" [totalPages]="p.totalPages" (pageChange)="load($event)" />
      }
    }
  `,
  styles: [
    `
      .header-row { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; }
      .lookup-form { display: flex; gap: 0.5rem; }
      .lookup-form input { width: 280px; }
      .filter-row { display: flex; gap: 1rem; align-items: center; margin: 1rem 0; }
      .filter-row label { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.8rem; color: var(--color-muted); }
      .mono { font-family: var(--font-mono); font-size: 0.82rem; }
    `,
  ],
})
export class AdminShipmentListComponent implements OnInit {
  private readonly shipmentService = inject(ShipmentService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly page = signal<PagedResult<ShipmentDto> | null>(null);
  readonly loading = signal(true);

  readonly shipmentStatusLabel = shipmentStatusLabel;
  readonly shipmentStatusTone = shipmentStatusTone;
  readonly statusOptions = Object.values(ShipmentStatus).filter((v): v is ShipmentStatus => typeof v === 'number');

  readonly statusFilter = this.fb.control<ShipmentStatus | null>(null);

  readonly lookupForm = this.fb.nonNullable.group({
    shipmentId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.load(1);
  }

  load(pageNumber: number): void {
    this.loading.set(true);
    this.shipmentService
      .getShipments(pageNumber, 20, this.statusFilter.value)
      .subscribe((result) => {
        this.page.set(result);
        this.loading.set(false);
      });
  }

  lookup(): void {
    if (this.lookupForm.invalid) return;
    this.router.navigate(['/admin/shipments', this.lookupForm.getRawValue().shipmentId]);
  }
}