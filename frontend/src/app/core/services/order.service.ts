import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateOrderRequest, OrderDto } from '../models/order.models';
import { AuthService } from './auth.service';
import { OrderHistoryService } from './order-history.service';

function generateIdempotencyKey(): string {
  // crypto.randomUUID is available in all evergreen browsers this app targets.
  return crypto.randomUUID();
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly baseUrl = `${environment.apiBaseUrl}/orders`;
  private readonly auth = inject(AuthService);
  private readonly orderHistory = inject(OrderHistoryService);

  constructor(private readonly http: HttpClient) {}

  /**
   * FR-3.2/3.4. The Idempotency-Key header is required by the backend (see
   * OrdersController's doc comment) — generated once per checkout attempt here so retrying
   * the same HTTP call (e.g. a flaky connection) never double-places the order. If the user
   * explicitly wants to submit a *new* order, call this again to get a fresh key.
   */
  create(request: CreateOrderRequest): Observable<OrderDto> {
    return this.http
      .post<OrderDto>(this.baseUrl, request, {
        headers: { 'Idempotency-Key': generateIdempotencyKey() },
      })
      .pipe(
        tap((order) => {
          const customerId = this.auth.currentUser()?.userId;
          if (customerId) this.orderHistory.record(customerId, order.id);
        })
      );
  }

  getById(id: string): Observable<OrderDto> {
    return this.http.get<OrderDto>(`${this.baseUrl}/${id}`);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {});
  }
}
