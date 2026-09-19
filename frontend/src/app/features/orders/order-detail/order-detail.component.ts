
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderDto } from '../../../core/models/order.models';
import { OrderStatus, UserRole } from '../../../core/models/enums';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../../shared/services/notification.service';
import {
  orderStatusLabel,
  orderStatusTone,
  paymentStatusLabel,
  paymentStatusTone,
} from '../../../shared/status-tone';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, StatusBadgeComponent],
  template: `
    @if (loading()) {
      <p class="hint-text">Loading order…</p>
    } @else {
      @if (order(); as o) {
        <div class="header-row">
          <div>
            <h1>Order <span class="mono">{{ o.id }}</span></h1>
            <p class="hint-text">Placed {{ o.createdAt | date: 'medium' }}</p>
          </div>

          @if (canCancel(o)) {
            <button
              class="btn-danger"
              (click)="cancel(o.id)"
              [disabled]="cancelling()"
            >
              {{ cancelling() ? 'Cancelling…' : 'Cancel order' }}
            </button>
          }
        </div>

        <div class="status-row">
          <app-status-badge
            [label]="orderStatusLabel(o.status)"
            [tone]="orderStatusTone(o.status)"
          />

          <app-status-badge
            [label]="paymentStatusLabel(o.paymentStatus)"
            [tone]="paymentStatusTone(o.paymentStatus)"
          />

          <span class="hint-text">
            {{ paymentMethodLabel(o) }}
          </span>
        </div>

        <table class="manifest-table">
          <thead>
            <tr>
              <th>Product</th>
              <th>Qty</th>
              <th>Unit price</th>
              <th>Line total</th>
            </tr>
          </thead>

          <tbody>
            @for (item of o.items; track item.productId) {
              <tr>
                <td>{{ item.productName }}</td>
                <td>{{ item.quantity }}</td>
                <td>{{ item.unitPrice | number: '1.2-2' }}</td>
                <td>{{ item.lineTotal | number: '1.2-2' }}</td>
              </tr>
            }
          </tbody>

          <tfoot>
            <tr>
              <td
                colspan="3"
                style="text-align: right; font-weight: 600;"
              >
                Total
              </td>

              <td style="font-weight: 600;">
                {{ o.totalAmount | number: '1.2-2' }}
              </td>
            </tr>
          </tfoot>
        </table>

        <p class="hint-text back-link">
          <a routerLink="/orders">← Back to my orders</a>
        </p>
      } @else {
        <div class="empty-state">
          Order not found, or you don't have access to it.
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
        gap: 1rem;
      }

      .mono {
        font-family: var(--font-mono);
        font-size: 0.85rem;
      }

      .status-row {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        margin: 1rem 0 1.25rem;
      }

      .back-link {
        margin-top: 1rem;
      }
    `,
  ],
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  readonly order = signal<OrderDto | null>(null);
  readonly loading = signal(true);
  readonly cancelling = signal(false);

  readonly orderStatusLabel = orderStatusLabel;
  readonly orderStatusTone = orderStatusTone;
  readonly paymentStatusLabel = paymentStatusLabel;
  readonly paymentStatusTone = paymentStatusTone;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.loading.set(false);
      return;
    }

    this.orderService.getById(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  paymentMethodLabel(order: OrderDto): string {
    return (
      ['Cash on delivery', 'Card', 'Wallet', 'Bank transfer'][
        order.paymentMethod
      ] ?? 'Unknown'
    );
  }

  /**
   * Mirrors the backend's own guard:
   * Customer or Admin, and order not already terminal.
   */
  canCancel(order: OrderDto): boolean {
    const role = this.auth.role();

    const isOwnerOrAdmin =
      role === UserRole.Admin ||
      (role === UserRole.Customer &&
        order.customerId === this.auth.currentUser()?.userId);

    const isTerminal =
      order.status === OrderStatus.Completed ||
      order.status === OrderStatus.Cancelled;

    return isOwnerOrAdmin && !isTerminal;
  }

  cancel(orderId: string): void {
    if (
      !window.confirm(
        'Cancel this order? Reserved inventory will be released.'
      )
    ) {
      return;
    }

    this.cancelling.set(true);

    this.orderService.cancel(orderId).subscribe({
      next: () => {
        this.notifications.success('Order cancelled.');

        this.orderService.getById(orderId).subscribe({
          next: (order) => {
            this.order.set(order);
            this.cancelling.set(false);
          },
          error: () => {
            this.cancelling.set(false);
          },
        });
      },
      error: () => {
        this.cancelling.set(false);
      },
    });
  }
}

