import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssignShipmentRequest,
  CancelShipmentRequest,
  ReturnShipmentRequest,
  ShipmentDto,
  UpdateShipmentStatusRequest,
} from '../models/shipment.models';
import { RecentLookupService, RECENT_SHIPMENTS_NAMESPACE } from './recent-lookup.service';

@Injectable({ providedIn: 'root' })
export class ShipmentService {
  private readonly baseUrl = `${environment.apiBaseUrl}/shipments`;
  private readonly recent = inject(RecentLookupService);

  constructor(private readonly http: HttpClient) {}

  getById(id: string): Observable<ShipmentDto> {
    return this.http
      .get<ShipmentDto>(`${this.baseUrl}/${id}`)
      .pipe(tap((shipment) => this.recent.record(RECENT_SHIPMENTS_NAMESPACE, shipment.id)));
  }

  /** FR-7.2/7.4 — the signed-in agent's own assigned shipments. */
  getMyAssignedShipments(): Observable<ShipmentDto[]> {
    return this.http.get<ShipmentDto[]>(`${this.baseUrl}/agents/me`);
  }

  /** Admin only — FR-6.3. */
  assign(id: string, request: AssignShipmentRequest): Observable<ShipmentDto> {
    return this.http
      .post<ShipmentDto>(`${this.baseUrl}/${id}/assign`, request)
      .pipe(tap((shipment) => this.recent.record(RECENT_SHIPMENTS_NAMESPACE, shipment.id)));
  }

  /** Admin or the shipment's assigned agent — FR-6.2/7.3/7.5. */
  updateStatus(id: string, request: UpdateShipmentStatusRequest): Observable<ShipmentDto> {
    return this.http.post<ShipmentDto>(`${this.baseUrl}/${id}/status`, request);
  }

  /** Admin only. */
  cancel(id: string, request: CancelShipmentRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, request);
  }

  /** Admin or the shipment's assigned agent — FR-6.4. */
  return(id: string, request: ReturnShipmentRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/return`, request);
  }
}
