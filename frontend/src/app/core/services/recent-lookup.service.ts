import { Injectable } from '@angular/core';

export const RECENT_SHIPMENTS_NAMESPACE = 'shipments';
export const RECENT_AGENTS_NAMESPACE = 'delivery-agents';

/**
 * The admin dashboard now lists shipments via the paged GET /api/v1/shipments endpoint, but
 * OrderDto still carries no ShipmentId to bridge from an order to its shipment, and a
 * Customer/Agent may still want to jump straight back to something they only have the ID
 * for (e.g. from a webhook payload or a support ticket). This remembers IDs seen in this
 * browser to back that quick "open by ID" lookup — it's a convenience, not the primary
 * listing mechanism anymore.
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