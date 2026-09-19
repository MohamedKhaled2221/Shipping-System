import { Injectable } from '@angular/core';

/**
 * FR-2.3 asks for order history, but OrdersController only exposes POST / and GET /{id} —
 * there is no GET /api/v1/orders that scopes to the calling customer. Until that endpoint
 * exists, this records the IDs of orders placed from this browser (per customer) so the
 * "My orders" screen has something to list, resolving each via GetOrderById. Swap this out
 * for a real "list my orders" call the moment the backend adds one.
 */
@Injectable({ providedIn: 'root' })
export class OrderHistoryService {
  private key(customerId: string): string {
    return `shipping.orderHistory.${customerId}`;
  }

  record(customerId: string, orderId: string): void {
    const ids = this.list(customerId);
    if (!ids.includes(orderId)) {
      localStorage.setItem(this.key(customerId), JSON.stringify([orderId, ...ids]));
    }
  }

  list(customerId: string): string[] {
    try {
      const raw = localStorage.getItem(this.key(customerId));
      return raw ? (JSON.parse(raw) as string[]) : [];
    } catch {
      return [];
    }
  }
}
