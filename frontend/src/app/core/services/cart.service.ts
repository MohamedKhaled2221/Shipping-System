import { Injectable, computed, signal } from '@angular/core';
import { ProductDto } from '../models/product.models';

export interface CartLine {
  product: ProductDto;
  quantity: number;
}

/**
 * Purely client-side order builder. There is no server-side "cart" concept in the SRS —
 * FR-3.2 has the customer submit the whole item list in one CreateOrderRequest — so this
 * just accumulates lines in memory until checkout submits them all at once.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly linesSignal = signal<CartLine[]>([]);

  readonly lines = this.linesSignal.asReadonly();
  readonly itemCount = computed(() => this.linesSignal().reduce((sum, l) => sum + l.quantity, 0));
  readonly total = computed(() =>
    this.linesSignal().reduce((sum, l) => sum + l.quantity * l.product.price, 0)
  );

  add(product: ProductDto, quantity: number): void {
    if (quantity <= 0) return;
    const lines = this.linesSignal();
    const existing = lines.find((l) => l.product.id === product.id);
    if (existing) {
      this.linesSignal.set(
        lines.map((l) => (l.product.id === product.id ? { ...l, quantity: l.quantity + quantity } : l))
      );
    } else {
      this.linesSignal.set([...lines, { product, quantity }]);
    }
  }

  updateQuantity(productId: string, quantity: number): void {
    if (quantity <= 0) {
      this.remove(productId);
      return;
    }
    this.linesSignal.set(this.linesSignal().map((l) => (l.product.id === productId ? { ...l, quantity } : l)));
  }

  remove(productId: string): void {
    this.linesSignal.set(this.linesSignal().filter((l) => l.product.id !== productId));
  }

  clear(): void {
    this.linesSignal.set([]);
  }
}
