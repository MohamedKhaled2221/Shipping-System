// Mirrors src/Domain/Enums/*.cs exactly — numeric values matter because System.Text.Json
// serializes C# enums as integers by default, and the backend has not opted into string
// enum converters. Keep these in lockstep with the backend if that ever changes.

export enum UserRole {
  Customer = 0,
  Admin = 1,
  DeliveryAgent = 2,
}

export enum PaymentMethod {
  CashOnDelivery = 0,
  Card = 1,
  Wallet = 2,
  BankTransfer = 3,
}

export enum OrderStatus {
  Pending = 0,
  Confirmed = 1,
  Processing = 2,
  Completed = 3,
  Cancelled = 4,
}

export enum PaymentStatus {
  Pending = 0,
  Paid = 1,
  Failed = 2,
  Refunded = 3,
  PartiallyRefunded = 4,
}

export enum ShipmentStatus {
  Pending = 0,
  Confirmed = 1,
  Preparing = 2,
  AssignedToCourier = 3,
  PickedUp = 4,
  OutForDelivery = 5,
  Delivered = 6,
  Failed = 7,
  Cancelled = 8,
  Returned = 9,
}

/** FR-6.7 state machine — used to only ever offer valid next statuses in the UI.
 *  The backend is the source of truth and re-validates regardless; this is a UX nicety. */
export const SHIPMENT_STATUS_TRANSITIONS: Record<ShipmentStatus, ShipmentStatus[]> = {
  [ShipmentStatus.Pending]: [ShipmentStatus.Confirmed, ShipmentStatus.Cancelled],
  [ShipmentStatus.Confirmed]: [ShipmentStatus.Preparing, ShipmentStatus.Cancelled],
  [ShipmentStatus.Preparing]: [ShipmentStatus.AssignedToCourier, ShipmentStatus.Cancelled],
  [ShipmentStatus.AssignedToCourier]: [ShipmentStatus.PickedUp, ShipmentStatus.Cancelled],
  [ShipmentStatus.PickedUp]: [ShipmentStatus.OutForDelivery, ShipmentStatus.Cancelled],
  [ShipmentStatus.OutForDelivery]: [ShipmentStatus.Delivered, ShipmentStatus.Failed, ShipmentStatus.Cancelled],
  [ShipmentStatus.Failed]: [ShipmentStatus.OutForDelivery, ShipmentStatus.Returned],
  [ShipmentStatus.Delivered]: [],
  [ShipmentStatus.Cancelled]: [],
  [ShipmentStatus.Returned]: [],
};

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  [OrderStatus.Pending]: 'Pending',
  [OrderStatus.Confirmed]: 'Confirmed',
  [OrderStatus.Processing]: 'Processing',
  [OrderStatus.Completed]: 'Completed',
  [OrderStatus.Cancelled]: 'Cancelled',
};

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  [PaymentStatus.Pending]: 'Pending',
  [PaymentStatus.Paid]: 'Paid',
  [PaymentStatus.Failed]: 'Failed',
  [PaymentStatus.Refunded]: 'Refunded',
  [PaymentStatus.PartiallyRefunded]: 'Partially refunded',
};

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  [PaymentMethod.CashOnDelivery]: 'Cash on delivery',
  [PaymentMethod.Card]: 'Card',
  [PaymentMethod.Wallet]: 'Wallet',
  [PaymentMethod.BankTransfer]: 'Bank transfer',
};

export const SHIPMENT_STATUS_LABELS: Record<ShipmentStatus, string> = {
  [ShipmentStatus.Pending]: 'Pending',
  [ShipmentStatus.Confirmed]: 'Confirmed',
  [ShipmentStatus.Preparing]: 'Preparing',
  [ShipmentStatus.AssignedToCourier]: 'Assigned to courier',
  [ShipmentStatus.PickedUp]: 'Picked up',
  [ShipmentStatus.OutForDelivery]: 'Out for delivery',
  [ShipmentStatus.Delivered]: 'Delivered',
  [ShipmentStatus.Failed]: 'Failed',
  [ShipmentStatus.Cancelled]: 'Cancelled',
  [ShipmentStatus.Returned]: 'Returned',
};

export const ROLE_LABELS: Record<UserRole, string> = {
  [UserRole.Customer]: 'Customer',
  [UserRole.Admin]: 'Admin',
  [UserRole.DeliveryAgent]: 'Delivery agent',
};
