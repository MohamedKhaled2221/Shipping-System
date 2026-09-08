using ShippingSystem.Domain.Common;

namespace ShippingSystem.Domain.Entities;

/// <summary>FR-7.5. One row per failed delivery attempt on a Shipment.</summary>
public sealed class FailedDeliveryReason : Entity<Guid>
{
    public Guid ShipmentId { get; private set; }
    public string Reason { get; private set; } = default!;
    public DateTime RecordedAt { get; private set; }

    private FailedDeliveryReason() { } // EF Core

    internal static FailedDeliveryReason Create(Guid shipmentId, string reason) =>
        new()
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Reason = reason,
            RecordedAt = DateTime.UtcNow
        };
}
