// Mirrors Api/Contracts/OrderRequests.cs and Application/Common/Models/SharedDtos.cs
import { OrderStatus, PaymentMethod, PaymentStatus } from './enums';

export interface OrderItemLineRequest {
  productId: string;
  quantity: number;
}

/**
 * CustomerId and ShippingAddressId: the backend takes CustomerId from the JWT, never from
 * this body. ShippingAddressId still has to be a real ShippingAddress.Id in the database —
 * the SRS data model (section 6) defines the ShippingAddress entity, but v1 of this backend
 * ships no /api/v1/addresses controller to create or list them yet. Until that endpoint
 * exists, the checkout screen asks the customer to paste an address ID directly. Flagging
 * that gap is exactly what SRS section 8 asks for — add the missing controller and swap the
 * checkout form's free-text ID field for a real address picker once it exists.
 */
export interface CreateOrderRequest {
  shippingAddressId: string;
  paymentMethod: PaymentMethod;
  items: OrderItemLineRequest[];
}

export interface OrderItemDto {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  customerId: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  paymentMethod: PaymentMethod;
  totalAmount: number;
  createdAt: string;
  items: OrderItemDto[];
}
