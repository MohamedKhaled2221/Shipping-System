using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Shipments.Common;

/// <summary>
/// Single mapping point Shipment (aggregate) -> ShipmentDto, so every handler in this module
/// (queries and commands that return the updated shipment) produces an identical shape.
/// </summary>
internal static class ShipmentMapping
{
    public static ShipmentDto ToDto(Shipment shipment) =>
        new(
            shipment.Id,
            shipment.OrderId,
            shipment.TrackingNumber,
            shipment.ShippingAddressId,
            shipment.ShippingFee,
            shipment.Status,
            shipment.DeliveryAgentId,
            shipment.EstimatedDeliveryDate,
            shipment.CreatedAt,
            shipment.ProviderName,
            shipment.ProviderReference,
            shipment.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new ShipmentStatusHistoryDto(h.Status, h.ChangedAt, h.ChangedBy, h.Notes))
                .ToList(),
            shipment.FailedDeliveryReasons
                .OrderBy(f => f.RecordedAt)
                .Select(f => f.Reason)
                .ToList());
}
