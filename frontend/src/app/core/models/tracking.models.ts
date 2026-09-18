// Mirrors the public tracking shape (FR-9.2) from Application/Common/Models/SharedDtos.cs
import { ShipmentStatus } from './enums';
import { ShipmentStatusHistoryDto } from './shipment.models';

export interface ShipmentTrackingDto {
  trackingNumber: string;
  currentStatus: ShipmentStatus;
  deliveryAgentId: string | null;
  estimatedDeliveryDate: string | null;
  history: ShipmentStatusHistoryDto[];
}
