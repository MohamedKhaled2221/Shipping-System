import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="hero">
      <p class="hero__eyebrow">Product Shipping &amp; Delivery Management</p>
      <h1>From checkout to the doorstep, one manifest at a time.</h1>
      <p class="hero__lede">
        Browse the catalog, place an order, and follow it through reservation, payment,
        dispatch and delivery — or drop straight into a tracking number if that's all you need.
      </p>
      <div class="hero__actions">
        <a routerLink="/products" class="btn btn-amber">Browse the catalog</a>
        <a routerLink="/track" class="btn btn-outline">Track a shipment</a>
      </div>
    </section>

    @if (!auth.isLoggedIn()) {
      <section class="roles">
        <div class="card">
          <h3>Customers</h3>
          <p>Place orders, watch payment and shipment status, and track deliveries end to end.</p>
          <a routerLink="/register" class="btn-outline btn-sm">Create an account</a>
        </div>
        <div class="card">
          <h3>Admin &amp; shipping staff</h3>
          <p>Assign shipments to agents, move them through the delivery state machine, manage returns.</p>
          <a routerLink="/login" [queryParams]="{ role: 'admin' }" class="btn-outline btn-sm">Admin sign in</a>
        </div>
        <div class="card">
          <h3>Delivery agents</h3>
          <p>See what's assigned to you today and update status as you go.</p>
          <a routerLink="/login" [queryParams]="{ role: 'agent' }" class="btn-outline btn-sm">Agent sign in</a>
        </div>
      </section>
    }
  `,
  styles: [
    `
      .hero { max-width: 640px; margin-bottom: 2.5rem; }
      .hero__eyebrow { font-family: var(--font-mono); font-size: 0.78rem; color: var(--color-amber-dark); margin-bottom: 0.5rem; }
      .hero h1 { font-size: 2.1rem; line-height: 1.15; }
      .hero__lede { color: var(--color-muted); margin: 0.75rem 0 1.25rem; }
      .hero__actions { display: flex; gap: 0.75rem; }
      .roles { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 1rem; }
      .roles .card p { color: var(--color-muted); font-size: 0.88rem; min-height: 3.4em; }
    `,
  ],
})
export class HomeComponent {
  readonly auth = inject(AuthService);
}
