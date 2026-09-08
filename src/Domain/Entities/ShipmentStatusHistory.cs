using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Domain.Entities;

/// <summary>FR-6.5, FR-9.2. Immutable audit log entry — one row per status change.</summary>
public sealed class ShipmentStatusHistory : Entity<Guid>
{
    public Guid ShipmentId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string ChangedBy { get; private set; } = default!;
    public string? Notes { get; private set; }

    private ShipmentStatusHistory() { } // EF Core

    internal static ShipmentStatusHistory Create(Guid shipmentId, ShipmentStatus status, string changedBy, string? notes) =>
        new()
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Status = status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
            Notes = notes
        };
}
