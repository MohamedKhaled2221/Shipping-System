// Mirrors Api/Contracts/ShipmentRequests.cs and Application/Common/Models/SharedDtos.cs
import { ShipmentStatus } from './enums';

export interface AssignShipmentRequest {
  deliveryAgentId: string;
}

/** Notes is required by the backend validator when newStatus is Failed (FR-7.5). */
export interface UpdateShipmentStatusRequest {
  newStatus: ShipmentStatus;
  notes: string | null;
}

export interface CancelShipmentRequest {
  reason: string | null;
}

export interface ReturnShipmentRequest {
  reason: string;
}

export interface ShipmentStatusHistoryDto {
  status: ShipmentStatus;
  changedAt: string;
  changedBy: string;
  notes: string | null;
}

export interface ShipmentDto {
  id: string;
  orderId: string;
  trackingNumber: string;
  shippingAddressId: string;
  shippingFee: number;
  status: ShipmentStatus;
  deliveryAgentId: string | null;
  estimatedDeliveryDate: string | null;
  createdAt: string;
  providerName: string | null;
  providerReference: string | null;
  statusHistory: ShipmentStatusHistoryDto[];
  failedDeliveryReasons: string[];
}
