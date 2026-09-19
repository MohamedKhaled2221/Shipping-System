import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ShipmentTrackingDto } from '../models/tracking.models';

@Injectable({ providedIn: 'root' })
export class TrackingService {
  private readonly baseUrl = `${environment.apiBaseUrl}/tracking`;

  constructor(private readonly http: HttpClient) {}

  /** FR-9.2 — public, no authentication required. */
  getByTrackingNumber(trackingNumber: string): Observable<ShipmentTrackingDto> {
    return this.http.get<ShipmentTrackingDto>(`${this.baseUrl}/${encodeURIComponent(trackingNumber)}`);
  }
}
