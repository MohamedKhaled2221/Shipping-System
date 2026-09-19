import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DeliveryAgentDto,
  RegisterDeliveryAgentRequest,
  SetAgentAvailabilityRequest,
  UpdateAgentProfileRequest,
} from '../models/delivery-agent.models';
import { RECENT_AGENTS_NAMESPACE, RecentLookupService } from './recent-lookup.service';

@Injectable({ providedIn: 'root' })
export class DeliveryAgentService {
  private readonly baseUrl = `${environment.apiBaseUrl}/delivery-agents`;
  private readonly recent = inject(RecentLookupService);

  constructor(private readonly http: HttpClient) {}

  /** Admin only — onboards the account; the agent logs in separately via /auth/agents/login. */
  register(request: RegisterDeliveryAgentRequest): Observable<DeliveryAgentDto> {
    return this.http
      .post<DeliveryAgentDto>(this.baseUrl, request)
      .pipe(tap((agent) => this.recent.record(RECENT_AGENTS_NAMESPACE, agent.id)));
  }

  /** Admin only — FR-6.3 support, the pool to assign a shipment from. */
  getAvailable(): Observable<DeliveryAgentDto[]> {
    return this.http.get<DeliveryAgentDto[]>(`${this.baseUrl}/available`);
  }

  getById(id: string): Observable<DeliveryAgentDto> {
    return this.http.get<DeliveryAgentDto>(`${this.baseUrl}/${id}`);
  }

  getMyProfile(): Observable<DeliveryAgentDto> {
    return this.http.get<DeliveryAgentDto>(`${this.baseUrl}/me`);
  }

  updateMyProfile(request: UpdateAgentProfileRequest): Observable<DeliveryAgentDto> {
    return this.http.put<DeliveryAgentDto>(`${this.baseUrl}/me`, request);
  }

  setMyAvailability(request: SetAgentAvailabilityRequest): Observable<DeliveryAgentDto> {
    return this.http.put<DeliveryAgentDto>(`${this.baseUrl}/me/availability`, request);
  }
}
