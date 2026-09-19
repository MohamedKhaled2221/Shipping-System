import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PaymentMethod, PAYMENT_METHOD_LABELS } from '../../../core/models/enums';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <h1>Checkout</h1>

    @if (cart.lines().length === 0) {
      <div class="empty-state">
        Your order is empty. <a routerLink="/products">Browse the catalog</a> to add items.
      </div>
    } @else {
      <div class="layout">
        <div class="card lines">
          <table class="manifest-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Qty</th>
                <th>Unit price</th>
                <th>Line total</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (line of cart.lines(); track line.product.id) {
                <tr>
                  <td>{{ line.product.name }}</td>
                  <td>
                    <input
                      type="number"
                      min="1"
                      [max]="line.product.quantityAvailable"
                      [value]="line.quantity"
                      (change)="updateQuantity(line.product.id, $any($event.target).value)"
                      class="qty-input"
                    />
                  </td>
                  <td>{{ line.product.price | number: '1.2-2' }}</td>
                  <td>{{ line.product.price * line.quantity | number: '1.2-2' }}</td>
                  <td><button class="btn-outline btn-sm" (click)="cart.remove(line.product.id)">Remove</button></td>
                </tr>
              }
            </tbody>
          </table>
          <div class="total-row">
            <span>Total</span>
            <strong>{{ cart.total() | number: '1.2-2' }} EGP</strong>
          </div>
        </div>

        <form class="card order-form" [formGroup]="form" (ngSubmit)="submit()">
          <h3>Delivery details</h3>

          <div class="field">
            <label for="addressId">Shipping address ID</label>
            <input id="addressId" formControlName="shippingAddressId" placeholder="Paste a ShippingAddress GUID" />
            <p class="hint-text">
              There's no address book endpoint in this API build yet — paste the ID of an existing
              ShippingAddress record. Wire this up to a real address picker once
              <code>/api/v1/addresses</code> exists.
            </p>
          </div>

          <div class="field">
            <label for="paymentMethod">Payment method</label>
            <select id="paymentMethod" formControlName="paymentMethod">
              @for (method of paymentMethods; track method) {
                <option [ngValue]="method">{{ paymentMethodLabels[method] }}</option>
              }
            </select>
          </div>

          <button type="submit" class="btn btn-amber" [disabled]="form.invalid || submitting()">
            {{ submitting() ? 'Placing order…' : 'Place order' }}
          </button>
        </form>
      </div>
    }
  `,
  styles: [
    `
      .layout { display: grid; grid-template-columns: 1.6fr 1fr; gap: 1.5rem; align-items: start; }
      .qty-input { width: 60px; }
      .total-row { display: flex; justify-content: space-between; padding-top: 0.9rem; font-size: 1rem; }
      .order-form button[type='submit'] { width: 100%; margin-top: 0.5rem; }
      @media (max-width: 780px) { .layout { grid-template-columns: 1fr; } }
    `,
  ],
})
export class CheckoutComponent {
  private readonly fb = inject(FormBuilder);
  private readonly orderService = inject(OrderService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly cart = inject(CartService);
  readonly submitting = signal(false);
  readonly paymentMethods = [
    PaymentMethod.CashOnDelivery,
    PaymentMethod.Card,
    PaymentMethod.Wallet,
    PaymentMethod.BankTransfer,
  ];
  readonly paymentMethodLabels = PAYMENT_METHOD_LABELS;

  readonly form = this.fb.nonNullable.group({
    shippingAddressId: ['', Validators.required],
    paymentMethod: [PaymentMethod.CashOnDelivery, Validators.required],
  });

  updateQuantity(productId: string, rawValue: string): void {
    const quantity = Number(rawValue);
    if (Number.isNaN(quantity)) return;
    this.cart.updateQuantity(productId, quantity);
  }

  submit(): void {
    if (this.form.invalid || this.cart.lines().length === 0) return;
    const { shippingAddressId, paymentMethod } = this.form.getRawValue();

    this.submitting.set(true);
    this.orderService
      .create({
        shippingAddressId,
        paymentMethod: Number(paymentMethod),
        items: this.cart.lines().map((l) => ({ productId: l.product.id, quantity: l.quantity })),
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (order) => {
          this.notifications.success('Order placed.');
          this.cart.clear();
          this.router.navigate(['/orders', order.id]);
        },
      });
  }
}
