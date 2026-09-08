using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Common.Models;

/// <summary>FR-3.1, FR-5.x — the Product catalog entry (Module 4).</summary>
public sealed record ProductDto(Guid Id, string Name, decimal Price, int QuantityAvailable);

/// <summary>Generic page envelope; introduced for Products (Module 4) but reusable by any future list endpoint.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record ShipmentStatusHistoryDto(ShipmentStatus Status, DateTime ChangedAt, string ChangedBy, string? Notes);

/// <summary>Shape returned by the public tracking endpoint (FR-9.2).</summary>
public sealed record ShipmentTrackingDto(
    string TrackingNumber,
    ShipmentStatus CurrentStatus,
    Guid? DeliveryAgentId,
    DateTime? EstimatedDeliveryDate,
    IReadOnlyList<ShipmentStatusHistoryDto> History);

/// <summary>FR-7.1 — a delivery agent's profile (contact info + availability status).</summary>
public sealed record DeliveryAgentDto(Guid Id, string Name, string Phone, bool IsAvailable, DateTime CreatedAt);

/// <summary>Full aggregate shape for Admin/Agent shipment-management screens (Module 7).</summary>
public sealed record ShipmentDto(
    Guid Id,
    Guid OrderId,
    string TrackingNumber,
    Guid ShippingAddressId,
    decimal ShippingFee,
    ShipmentStatus Status,
    Guid? DeliveryAgentId,
    DateTime? EstimatedDeliveryDate,
    DateTime CreatedAt,
    string? ProviderName,
    string? ProviderReference,
    IReadOnlyList<ShipmentStatusHistoryDto> StatusHistory,
    IReadOnlyList<string> FailedDeliveryReasons);
