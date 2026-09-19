import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { UserRole } from '../../core/models/enums';
import { PagedResult, ProductDto } from '../../core/models/product.models';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { ProductService } from '../../core/services/product.service';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { NotificationService } from '../../shared/services/notification.service';

@Component({
  selector: 'app-product-catalog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, PaginationComponent],
  template: `
    <div class="catalog-header">
      <h1>Catalog</h1>
      @if (isAdmin()) {
        <button class="btn btn-amber" (click)="showCreateForm.set(!showCreateForm())">
          {{ showCreateForm() ? 'Close' : '+ New product' }}
        </button>
      }
    </div>

    @if (showCreateForm()) {
      <form class="card create-form" [formGroup]="createForm" (ngSubmit)="createProduct()">
        <div class="field">
          <label for="name">Name</label>
          <input id="name" formControlName="name" />
        </div>
        <div class="field">
          <label for="price">Price</label>
          <input id="price" type="number" step="0.01" min="0" formControlName="price" />
        </div>
        <div class="field">
          <label for="qty">Initial quantity</label>
          <input id="qty" type="number" min="0" formControlName="initialQuantity" />
        </div>
        <button type="submit" class="btn btn-amber" [disabled]="createForm.invalid || creating()">
          {{ creating() ? 'Creating…' : 'Create product' }}
        </button>
      </form>
    }

    @if (loading()) {
      <p class="hint-text">Loading products…</p>
    } @else if (page()?.items?.length === 0) {
      <div class="empty-state">No products in the catalog yet.</div>
    } @else {
      <div class="grid">
        @for (product of page()?.items; track product.id) {
          <div class="card product-card">
            @if (editingId() === product.id) {
              <form [formGroup]="editForm" (ngSubmit)="saveEdit(product.id)">
                <div class="field">
                  <label>Name</label>
                  <input formControlName="name" />
                </div>
                <div class="field">
                  <label>Price</label>
                  <input type="number" step="0.01" min="0" formControlName="price" />
                </div>
                <div class="edit-actions">
                  <button type="submit" class="btn-sm btn-amber" [disabled]="editForm.invalid">Save</button>
                  <button type="button" class="btn-outline btn-sm" (click)="editingId.set(null)">Cancel</button>
                </div>
              </form>
            } @else {
              <h3>{{ product.name }}</h3>
              <p class="price">{{ product.price | number: '1.2-2' }} EGP</p>
              <p class="stock" [class.stock--low]="product.quantityAvailable === 0">
                {{ product.quantityAvailable }} in stock
              </p>

              @if (isAdmin()) {
                <div class="admin-actions">
                  <button class="btn-outline btn-sm" (click)="startEdit(product)">Edit</button>
                  <button class="btn-outline btn-sm" (click)="restock(product)">+ Restock</button>
                </div>
              } @else if (isCustomer()) {
                <div class="qty-row">
                  <input type="number" min="1" [max]="product.quantityAvailable" [(ngModel)]="quantities[product.id]" [ngModelOptions]="{ standalone: true }" />
                  <button
                    class="btn btn-amber btn-sm"
                    [disabled]="product.quantityAvailable === 0"
                    (click)="addToCart(product)"
                  >
                    Add to order
                  </button>
                </div>
              }
            }
          </div>
        }
      </div>

      @if (page(); as p) {
        <app-pagination [pageNumber]="p.pageNumber" [totalPages]="p.totalPages" (pageChange)="load($event)" />
      }
    }
  `,
  styles: [
    `
      .catalog-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; }
      .create-form { max-width: 420px; margin-bottom: 1.5rem; display: flex; flex-direction: column; gap: 0; }
      .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1rem; }
      .product-card h3 { margin-bottom: 0.2rem; }
      .price { font-family: var(--font-mono); font-weight: 600; color: var(--color-ink); margin: 0.2rem 0; }
      .stock { font-size: 0.8rem; color: var(--color-muted); margin: 0 0 0.75rem; }
      .stock--low { color: var(--color-red); }
      .qty-row { display: flex; gap: 0.5rem; }
      .qty-row input { width: 70px; }
      .admin-actions { display: flex; gap: 0.5rem; }
      .edit-actions { display: flex; gap: 0.5rem; margin-top: 0.5rem; }
    `,
  ],
})
export class ProductCatalogComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly page = signal<PagedResult<ProductDto> | null>(null);
  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly showCreateForm = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly quantities: Record<string, number> = {};

  readonly isAdmin = () => this.auth.role() === UserRole.Admin;
  readonly isCustomer = () => this.auth.role() === UserRole.Customer;

  readonly createForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    initialQuantity: [0, [Validators.required, Validators.min(0)]],
  });

  readonly editForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
  });

  ngOnInit(): void {
    this.load(1);
  }

  load(pageNumber: number): void {
    this.loading.set(true);
    this.productService.getProducts(pageNumber, 12).subscribe({
      next: (result) => {
        this.page.set(result);
        result.items.forEach((p) => (this.quantities[p.id] = this.quantities[p.id] ?? 1));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  createProduct(): void {
    if (this.createForm.invalid) return;
    this.creating.set(true);
    this.productService
      .create(this.createForm.getRawValue())
      .pipe(finalize(() => this.creating.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Product created.');
          this.createForm.reset({ name: '', price: 0, initialQuantity: 0 });
          this.showCreateForm.set(false);
          this.load(this.page()?.pageNumber ?? 1);
        },
      });
  }

  startEdit(product: ProductDto): void {
    this.editingId.set(product.id);
    this.editForm.setValue({ name: product.name, price: product.price });
  }

  saveEdit(id: string): void {
    if (this.editForm.invalid) return;
    this.productService.update(id, this.editForm.getRawValue()).subscribe({
      next: () => {
        this.notifications.success('Product updated.');
        this.editingId.set(null);
        this.load(this.page()?.pageNumber ?? 1);
      },
    });
  }

  restock(product: ProductDto): void {
    const raw = window.prompt(`Add how many units to "${product.name}"?`, '10');
    const quantity = Number(raw);
    if (!raw || Number.isNaN(quantity) || quantity <= 0) return;

    this.productService.restock(product.id, { quantity }).subscribe({
      next: () => {
        this.notifications.success('Stock updated.');
        this.load(this.page()?.pageNumber ?? 1);
      },
    });
  }

  addToCart(product: ProductDto): void {
    const quantity = this.quantities[product.id] ?? 1;
    this.cart.add(product, quantity);
    this.notifications.success(`Added ${quantity} × ${product.name} to your order.`);
  }
}
