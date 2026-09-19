import { Injectable } from '@angular/core';

export const RECENT_SHIPMENTS_NAMESPACE = 'shipments';
export const RECENT_AGENTS_NAMESPACE = 'delivery-agents';

/**
 * ShipmentsController exposes GetById, agents/me, assign, status, cancel, and return — there
 * is no paged "list all shipments" endpoint for Admin/shipping-employee yet, and OrderDto
 * carries no ShipmentId to bridge from an order to its shipment either. Until the backend
 * adds something like GET /api/v1/shipments?status=&agentId=&page=, the admin console can
 * only open a shipment it already has the ID for — this remembers IDs seen in this browser
 * (e.g. from a webhook payload, a support ticket, or one you just assigned) so there's a
 * working list to click back into. Flag this gap per SRS section 8; it's a real backend
 * requirement gap, not just a frontend simplification.
 */
@Injectable({ providedIn: 'root' })
export class RecentLookupService {
  private key(namespace: string): string {
    return `shipping.recent.${namespace}`;
  }

  record(namespace: string, id: string): void {
    const ids = this.list(namespace);
    if (!ids.includes(id)) {
      localStorage.setItem(this.key(namespace), JSON.stringify([id, ...ids].slice(0, 50)));
    }
  }

  list(namespace: string): string[] {
    try {
      const raw = localStorage.getItem(this.key(namespace));
      return raw ? (JSON.parse(raw) as string[]) : [];
    } catch {
      return [];
    }
  }

  remove(namespace: string, id: string): void {
    localStorage.setItem(this.key(namespace), JSON.stringify(this.list(namespace).filter((x) => x !== id)));
  }
}
