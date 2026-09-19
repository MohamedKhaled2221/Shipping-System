import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { OrderDto } from '../../../core/models/order.models';
import { AuthService } from '../../../core/services/auth.service';
import { OrderHistoryService } from '../../../core/services/order-history.service';
import { OrderService } from '../../../core/services/order.service';
import { orderStatusLabel, orderStatusTone, paymentStatusLabel, paymentStatusTone } from '../../../shared/status-tone';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, StatusBadgeComponent],
  template: `
    <div class="header-row">
      <h1>My orders</h1>
      <form [formGroup]="lookupForm" (ngSubmit)="lookupById()" class="lookup-form">
        <input formControlName="orderId" placeholder="Look up an order by ID" />
        <button type="submit" class="btn-outline btn-sm" [disabled]="lookupForm.invalid">Open</button>
      </form>
    </div>

    <p class="hint-text">
      This lists orders placed from this browser, since the API doesn't yet expose a
      "list my orders" endpoint — paste an order ID above if you have one from elsewhere.
    </p>

    @if (loading()) {
      <p class="hint-text">Loading…</p>
    } @else if (orders().length === 0) {
      <div class="empty-state">
        No orders yet. <a routerLink="/products">Browse the catalog</a> to place one.
      </div>
    } @else {
      <table class="manifest-table">
        <thead>
          <tr>
            <th>Order</th>
            <th>Placed</th>
            <th>Items</th>
            <th>Total</th>
            <th>Order status</th>
            <th>Payment</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (order of orders(); track order.id) {
            <tr>
              <td class="mono">{{ order.id.slice(0, 8) }}…</td>
              <td>{{ order.createdAt | date: 'medium' }}</td>
              <td>{{ order.items.length }}</td>
              <td>{{ order.totalAmount | number: '1.2-2' }}</td>
              <td><app-status-badge [label]="orderStatusLabel(order.status)" [tone]="orderStatusTone(order.status)" /></td>
              <td><app-status-badge [label]="paymentStatusLabel(order.paymentStatus)" [tone]="paymentStatusTone(order.paymentStatus)" /></td>
              <td><a [routerLink]="['/orders', order.id]" class="btn-outline btn-sm">View</a></td>
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
      .lookup-form input { width: 260px; }
      .mono { font-family: var(--font-mono); font-size: 0.82rem; }
    `,
  ],
})
export class OrderListComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly orderHistory = inject(OrderHistoryService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly orders = signal<OrderDto[]>([]);
  readonly loading = signal(true);

  readonly lookupForm = this.fb.nonNullable.group({
    orderId: ['', Validators.required],
  });

  readonly orderStatusLabel = orderStatusLabel;
  readonly orderStatusTone = orderStatusTone;
  readonly paymentStatusLabel = paymentStatusLabel;
  readonly paymentStatusTone = paymentStatusTone;

  ngOnInit(): void {
    const customerId = this.auth.currentUser()?.userId;
    const ids = customerId ? this.orderHistory.list(customerId) : [];

    if (ids.length === 0) {
      this.loading.set(false);
      return;
    }

    forkJoin(ids.map((id) => this.orderService.getById(id).pipe(catchError(() => of(null))))).subscribe((results) => {
      this.orders.set(results.filter((o): o is OrderDto => o !== null));
      this.loading.set(false);
    });
  }

  lookupById(): void {
    if (this.lookupForm.invalid) return;
    this.router.navigate(['/orders', this.lookupForm.getRawValue().orderId]);
  }
}
