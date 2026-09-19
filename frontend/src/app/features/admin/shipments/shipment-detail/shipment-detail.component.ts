import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  SHIPMENT_STATUS_TRANSITIONS,
  ShipmentStatus,
  UserRole,
} from '../../../../core/models/enums';
import { DeliveryAgentDto } from '../../../../core/models/delivery-agent.models';
import { ShipmentDto } from '../../../../core/models/shipment.models';
import { AuthService } from '../../../../core/services/auth.service';
import { DeliveryAgentService } from '../../../../core/services/delivery-agent.service';
import { ShipmentService } from '../../../../core/services/shipment.service';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { StatusTimelineComponent } from '../../../../shared/components/status-timeline/status-timeline.component';
import { NotificationService } from '../../../../shared/services/notification.service';
import {
  shipmentStatusLabel,
  shipmentStatusTone,
} from '../../../../shared/status-tone';

@Component({
  selector: 'app-shipment-detail',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    StatusBadgeComponent,
    StatusTimelineComponent,
  ],
  template: `
    @if (loading()) {
      <p class="hint-text">Loading shipment…</p>
    } @else {
      @if (shipment(); as s) {
        <div class="header-row">
          <div>
            <h1 class="mono">{{ s.trackingNumber }}</h1>

            <p class="hint-text">
              Order
              <a [routerLink]="['/orders', s.orderId]">
                {{ s.orderId }}
              </a>
            </p>
          </div>

          <app-status-badge
            [label]="shipmentStatusLabel(s.status)"
            [tone]="shipmentStatusTone(s.status)"
          />
        </div>

        <div class="layout">
          <div class="card">
            <h3>Details</h3>

            <dl class="detail-list">
              <dt>Shipping fee</dt>
              <dd>{{ s.shippingFee | number: '1.2-2' }}</dd>

              <dt>Provider</dt>
              <dd>{{ s.providerName ?? '—' }}</dd>

              <dt>Provider reference</dt>
              <dd class="mono">
                {{ s.providerReference ?? '—' }}
              </dd>

              <dt>Assigned agent</dt>
              <dd class="mono">
                {{ s.deliveryAgentId ?? 'Unassigned' }}
              </dd>

              <dt>Est. delivery</dt>
              <dd>
                {{
                  s.estimatedDeliveryDate
                    ? (s.estimatedDeliveryDate | date: 'mediumDate')
                    : '—'
                }}
              </dd>
            </dl>

            @if (s.failedDeliveryReasons.length > 0) {
              <h3>Failed delivery reasons</h3>

              <ul class="reasons">
                @for (reason of s.failedDeliveryReasons; track reason) {
                  <li>{{ reason }}</li>
                }
              </ul>
            }

            @if (isAdmin() && !s.deliveryAgentId) {
              <h3>Assign to agent</h3>

              <form
                [formGroup]="assignForm"
                (ngSubmit)="assign(s.id)"
                class="inline-form"
              >
                <select formControlName="deliveryAgentId">
                  <option value="" disabled>
                    Select an available agent…
                  </option>

                  @for (agent of availableAgents(); track agent.id) {
                    <option [value]="agent.id">
                      {{ agent.name }} — {{ agent.phone }}
                    </option>
                  }
                </select>

                <button
                  type="submit"
                  class="btn btn-amber btn-sm"
                  [disabled]="assignForm.invalid || assigning()"
                >
                  {{ assigning() ? 'Assigning…' : 'Assign' }}
                </button>
              </form>

              @if (availableAgents().length === 0) {
                <p class="hint-text">
                  No available agents right now.
                </p>
              }
            }

            @if (canUpdateStatus(s)) {
              <h3>Update status</h3>

              @if (nextStatuses(s.status).length === 0) {
                <p class="hint-text">
                  This shipment is in a terminal state.
                </p>
              } @else {
                <form
                  [formGroup]="statusForm"
                  (ngSubmit)="updateStatus(s.id)"
                  class="status-form"
                >
                  <select formControlName="newStatus">
                    @for (
                      status of nextStatuses(s.status);
                      track status
                    ) {
                      <option [ngValue]="status">
                        {{ shipmentStatusLabel(status) }}
                      </option>
                    }
                  </select>

                  <input
                    formControlName="notes"
                    [placeholder]="
                      statusForm.getRawValue().newStatus === failedStatus
                        ? 'Failure reason (required)'
                        : 'Notes (optional)'
                    "
                  />

                  <button
                    type="submit"
                    class="btn btn-amber btn-sm"
                    [disabled]="
                      statusForm.invalid || updatingStatus()
                    "
                  >
                    {{
                      updatingStatus()
                        ? 'Updating…'
                        : 'Update'
                    }}
                  </button>
                </form>
              }
            }

            <div class="danger-actions">
              @if (isAdmin() && isCancellable(s.status)) {
                <button
                  type="button"
                  class="btn-danger btn-sm"
                  (click)="cancel(s.id)"
                >
                  Cancel shipment
                </button>
              }

              @if (canReturn(s)) {
                <button
                  type="button"
                  class="btn-outline btn-sm"
                  (click)="returnShipment(s.id)"
                >
                  Mark returned
                </button>
              }
            </div>
          </div>

          <div class="card">
            <h3>Status history</h3>

            <app-status-timeline
              [history]="s.statusHistory"
            />
          </div>
        </div>
      } @else {
        <div class="empty-state">
          Shipment not found, or you don't have access to it.
        </div>
      }
    }
  `,

  styles: [
    `
      .header-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 1.25rem;
      }

      .mono {
        font-family: var(--font-mono);
      }

      .layout {
        display: grid;
        grid-template-columns: 1.1fr 1fr;
        gap: 1.25rem;
        align-items: start;
      }

      .detail-list {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 0.3rem 1rem;
        font-size: 0.88rem;
        margin: 0 0 1rem;
      }

      .detail-list dt {
        color: var(--color-muted);
      }

      .detail-list dd {
        margin: 0;
      }

      .reasons {
        font-size: 0.85rem;
        padding-left: 1.1rem;
      }

      .inline-form,
      .status-form {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-bottom: 0.75rem;
      }

      .status-form input {
        flex: 1;
        min-width: 160px;
      }

      .danger-actions {
        display: flex;
        gap: 0.6rem;
        margin-top: 1rem;
      }

      @media (max-width: 780px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class ShipmentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shipmentService = inject(ShipmentService);
  private readonly agentService = inject(DeliveryAgentService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly auth = inject(AuthService);

  readonly shipment = signal<ShipmentDto | null>(null);
  readonly availableAgents = signal<DeliveryAgentDto[]>([]);
  readonly loading = signal(true);
  readonly assigning = signal(false);
  readonly updatingStatus = signal(false);

  readonly shipmentStatusLabel = shipmentStatusLabel;
  readonly shipmentStatusTone = shipmentStatusTone;
  readonly failedStatus = ShipmentStatus.Failed;

  readonly isAdmin = () =>
    this.auth.role() === UserRole.Admin;

  readonly assignForm = this.fb.nonNullable.group({
    deliveryAgentId: ['', Validators.required],
  });

  readonly statusForm = this.fb.nonNullable.group({
    newStatus: [
      ShipmentStatus.Confirmed,
      Validators.required,
    ],
    notes: [''],
  });

  private shipmentId = '';

  ngOnInit(): void {
    this.shipmentId =
      this.route.snapshot.paramMap.get('id') ?? '';

    if (!this.shipmentId) {
      this.loading.set(false);
      return;
    }

    this.loadShipment();

    if (this.isAdmin()) {
      this.agentService.getAvailable().subscribe({
        next: (agents) =>
          this.availableAgents.set(agents),
      });
    }
  }

  private loadShipment(): void {
    this.loading.set(true);

    this.shipmentService.getById(this.shipmentId).subscribe({
      next: (shipment) => {
        this.shipment.set(shipment);

        this.statusForm.patchValue({
          newStatus:
            this.nextStatuses(shipment.status)[0] ??
            shipment.status,
        });

        this.loading.set(false);
      },

      error: () => {
        this.loading.set(false);
      },
    });
  }

  nextStatuses(
    current: ShipmentStatus,
  ): ShipmentStatus[] {
    return SHIPMENT_STATUS_TRANSITIONS[current] ?? [];
  }

  canUpdateStatus(s: ShipmentDto): boolean {
    const role = this.auth.role();

    if (role === UserRole.Admin) {
      return true;
    }

    if (role === UserRole.DeliveryAgent) {
      return (
        s.deliveryAgentId ===
        this.auth.currentUser()?.userId
      );
    }

    return false;
  }

  canReturn(s: ShipmentDto): boolean {
    return (
      s.status === ShipmentStatus.Failed &&
      this.canUpdateStatus(s)
    );
  }

  isCancellable(status: ShipmentStatus): boolean {
    return ![
      ShipmentStatus.Delivered,
      ShipmentStatus.Cancelled,
      ShipmentStatus.Returned,
    ].includes(status);
  }

  assign(shipmentId: string): void {
    if (this.assignForm.invalid) {
      return;
    }

    this.assigning.set(true);

    this.shipmentService
      .assign(
        shipmentId,
        this.assignForm.getRawValue(),
      )
      .pipe(
        finalize(() => this.assigning.set(false)),
      )
      .subscribe({
        next: (shipment) => {
          this.shipment.set(shipment);
          this.notifications.success(
            'Shipment assigned.',
          );
        },
      });
  }

  updateStatus(shipmentId: string): void {
    if (this.statusForm.invalid) {
      return;
    }

    const { newStatus, notes } =
      this.statusForm.getRawValue();

    this.updatingStatus.set(true);

    this.shipmentService
      .updateStatus(shipmentId, {
        newStatus: Number(newStatus),
        notes: notes || null,
      })
      .pipe(
        finalize(() =>
          this.updatingStatus.set(false),
        ),
      )
      .subscribe({
        next: (shipment) => {
          this.shipment.set(shipment);

          this.statusForm.patchValue({
            notes: '',
            newStatus:
              this.nextStatuses(shipment.status)[0] ??
              shipment.status,
          });

          this.notifications.success(
            'Status updated.',
          );
        },
      });
  }

  cancel(shipmentId: string): void {
    const reason =
      window.prompt(
        'Reason for cancelling this shipment (optional):',
      ) ?? '';

    this.shipmentService
      .cancel(shipmentId, {
        reason: reason || null,
      })
      .subscribe({
        next: () => {
          this.notifications.success(
            'Shipment cancelled.',
          );

          this.loadShipment();
        },
      });
  }

  returnShipment(shipmentId: string): void {
    const reason = window.prompt(
      'Reason for the return:',
    );

    if (!reason) {
      return;
    }

    this.shipmentService
      .return(shipmentId, { reason })
      .subscribe({
        next: () => {
          this.notifications.success(
            'Shipment marked as returned.',
          );

          this.loadShipment();
        },
      });
  }
}