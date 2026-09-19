import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';
import { ShipmentTrackingDto } from '../../core/models/tracking.models';
import { TrackingService } from '../../core/services/tracking.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { StatusTimelineComponent } from '../../shared/components/status-timeline/status-timeline.component';
import { shipmentStatusLabel, shipmentStatusTone } from '../../shared/status-tone';

@Component({
  selector: 'app-tracking-lookup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StatusBadgeComponent, StatusTimelineComponent],
  template: `
    <h1>Track a shipment</h1>
    <p class="hint-text">Enter the tracking number from your order confirmation — no account needed.</p>

    <form class="card lookup-card" [formGroup]="form" (ngSubmit)="lookup()">
      <div class="field">
        <label for="trackingNumber">Tracking number</label>
        <input id="trackingNumber" formControlName="trackingNumber" placeholder="TRK-2026-000001" class="mono" />
      </div>
      <button type="submit" class="btn btn-amber" [disabled]="form.invalid || loading()">
        {{ loading() ? 'Looking up…' : 'Track' }}
      </button>
    </form>

    @if (notFound()) {
      <div class="empty-state">No shipment found for that tracking number.</div>
    }

    @if (tracking(); as t) {
      <div class="card result-card">
        <div class="result-head">
          <div>
            <p class="hint-text">Tracking number</p>
            <p class="mono tracking-value">{{ t.trackingNumber }}</p>
          </div>
          <app-status-badge [label]="shipmentStatusLabel(t.currentStatus)" [tone]="shipmentStatusTone(t.currentStatus)" />
        </div>

        @if (t.estimatedDeliveryDate) {
          <p class="hint-text">Estimated delivery: {{ t.estimatedDeliveryDate | date: 'mediumDate' }}</p>
        }

        <h3>History</h3>
        <app-status-timeline [history]="t.history" />
      </div>
    }
  `,
  styles: [
    `
      .lookup-card { max-width: 420px; display: flex; align-items: flex-end; gap: 0.75rem; margin-bottom: 1.5rem; }
      .lookup-card .field { flex: 1; margin-bottom: 0; }
      .result-card { max-width: 560px; }
      .result-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
      .tracking-value { font-size: 1.1rem; font-weight: 600; }
    `,
  ],
})
export class TrackingLookupComponent {
  private readonly fb = inject(FormBuilder);
  private readonly trackingService = inject(TrackingService);
  private readonly route = inject(ActivatedRoute);

  readonly tracking = signal<ShipmentTrackingDto | null>(null);
  readonly loading = signal(false);
  readonly notFound = signal(false);

  readonly shipmentStatusLabel = shipmentStatusLabel;
  readonly shipmentStatusTone = shipmentStatusTone;

  readonly form = this.fb.nonNullable.group({
    trackingNumber: [this.route.snapshot.queryParamMap.get('n') ?? '', Validators.required],
  });

  constructor() {
    if (this.form.getRawValue().trackingNumber) this.lookup();
  }

  lookup(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.notFound.set(false);
    this.tracking.set(null);

    this.trackingService
      .getByTrackingNumber(this.form.getRawValue().trackingNumber)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => this.tracking.set(result),
        error: () => this.notFound.set(true),
      });
  }
}
