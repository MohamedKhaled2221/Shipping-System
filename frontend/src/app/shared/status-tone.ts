import {
  ORDER_STATUS_LABELS,
  OrderStatus,
  PAYMENT_STATUS_LABELS,
  PaymentStatus,
  SHIPMENT_STATUS_LABELS,
  ShipmentStatus,
} from '../core/models/enums';
import { StatusTone } from './components/status-badge/status-badge.component';

export function orderStatusTone(status: OrderStatus): StatusTone {
  switch (status) {
    case OrderStatus.Completed:
      return 'success';
    case OrderStatus.Cancelled:
      return 'danger';
    case OrderStatus.Processing:
    case OrderStatus.Confirmed:
      return 'progress';
    default:
      return 'neutral';
  }
}

export function paymentStatusTone(status: PaymentStatus): StatusTone {
  switch (status) {
    case PaymentStatus.Paid:
      return 'success';
    case PaymentStatus.Failed:
      return 'danger';
    case PaymentStatus.Refunded:
    case PaymentStatus.PartiallyRefunded:
      return 'warning';
    default:
      return 'neutral';
  }
}

export function shipmentStatusTone(status: ShipmentStatus): StatusTone {
  switch (status) {
    case ShipmentStatus.Delivered:
      return 'success';
    case ShipmentStatus.Failed:
    case ShipmentStatus.Cancelled:
      return 'danger';
    case ShipmentStatus.Returned:
      return 'warning';
    case ShipmentStatus.Pending:
      return 'neutral';
    default:
      return 'progress';
  }
}

export const orderStatusLabel = (s: OrderStatus) => ORDER_STATUS_LABELS[s];
export const paymentStatusLabel = (s: PaymentStatus) => PAYMENT_STATUS_LABELS[s];
export const shipmentStatusLabel = (s: ShipmentStatus) => SHIPMENT_STATUS_LABELS[s];
