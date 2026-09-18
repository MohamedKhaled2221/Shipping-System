import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { ROLE_LABELS, UserRole } from '../../core/models/enums';
import { ToastStackComponent } from '../../shared/components/toast/toast.component';

interface NavLink {
  path: string;
  label: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, ToastStackComponent],
  template: `
    <div class="shell">
      <header class="topbar">
        <a class="brand" routerLink="/">
          <span class="brand__mark">▣</span>
          <span class="brand__name">Consignment</span>
        </a>

        <nav class="nav">
          @for (link of navLinks(); track link.path) {
            <a [routerLink]="link.path" routerLinkActive="nav__link--active" class="nav__link">{{ link.label }}</a>
          }
        </nav>

        <div class="topbar__right">
          @if (auth.isLoggedIn()) {
            @if (role() === UserRole.Customer && cart.itemCount() > 0) {
              <a routerLink="/checkout" class="cart-pill">Cart · {{ cart.itemCount() }}</a>
            }
            <span class="role-pill">{{ roleLabel() }}</span>
            <button class="btn-outline btn-sm" (click)="logout()">Sign out</button>
          } @else {
            <a routerLink="/login" class="btn-outline btn-sm">Sign in</a>
            <a routerLink="/register" class="btn btn-amber btn-sm">Register</a>
          }
        </div>
      </header>

      <main class="content">
        <router-outlet />
      </main>

      <app-toast-stack />
    </div>
  `,
  styles: [
    `
      .shell { min-height: 100%; display: flex; flex-direction: column; }
      .topbar {
        display: flex;
        align-items: center;
        gap: 1.5rem;
        padding: 0.75rem 1.5rem;
        background: var(--color-ink);
        color: #fff;
        position: sticky;
        top: 0;
        z-index: 10;
      }
      .brand {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: #fff;
        text-decoration: none;
        font-family: var(--font-display);
        font-weight: 700;
        font-size: 1.1rem;
      }
      .brand__mark { color: var(--color-amber); }
      .nav { display: flex; gap: 1.1rem; flex: 1; }
      .nav__link {
        color: rgba(255, 255, 255, 0.75);
        text-decoration: none;
        font-size: 0.88rem;
        font-weight: 500;
        padding: 0.3em 0;
        border-bottom: 2px solid transparent;
      }
      .nav__link:hover { color: #fff; }
      .nav__link--active { color: #fff; border-bottom-color: var(--color-amber); }
      .topbar__right { display: flex; align-items: center; gap: 0.7rem; }
      .role-pill {
        font-family: var(--font-mono);
        font-size: 0.72rem;
        background: rgba(255, 255, 255, 0.12);
        padding: 0.3em 0.6em;
        border-radius: var(--radius-sm);
      }
      .cart-pill {
        font-family: var(--font-mono);
        font-size: 0.78rem;
        background: var(--color-amber);
        color: #12233d;
        padding: 0.3em 0.7em;
        border-radius: var(--radius-sm);
        text-decoration: none;
        font-weight: 600;
      }
      .topbar__right .btn-outline { color: #fff; border-color: rgba(255, 255, 255, 0.5); }
      .topbar__right .btn-outline:hover { background: rgba(255, 255, 255, 0.1); }
      .content { flex: 1; padding: 1.75rem; max-width: 1180px; width: 100%; margin: 0 auto; }
    `,
  ],
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  private readonly router = inject(Router);

  readonly UserRole = UserRole;
  readonly role = this.auth.role;

  readonly roleLabel = () => {
    const role = this.auth.role();
    return role === null ? '' : ROLE_LABELS[role];
  };

  readonly navLinks = () => {
    const role = this.auth.role();
    const links: NavLink[] = [
      { path: '/products', label: 'Catalog' },
      { path: '/track', label: 'Track a shipment' },
    ];

    if (role === UserRole.Customer) {
      links.push({ path: '/orders', label: 'My orders' });
    }
    if (role === UserRole.Admin) {
      links.push({ path: '/admin/shipments', label: 'Shipments' });
      links.push({ path: '/admin/delivery-agents', label: 'Delivery agents' });
    }
    if (role === UserRole.DeliveryAgent) {
      links.push({ path: '/agent/shipments', label: 'My deliveries' });
      links.push({ path: '/agent/profile', label: 'My profile' });
    }
    return links;
  };

  logout(): void {
    this.auth.logout();
    this.cart.clear();
    this.router.navigateByUrl('/');
  }
}
