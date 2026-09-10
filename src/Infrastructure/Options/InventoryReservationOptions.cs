namespace ShippingSystem.Infrastructure.Options;

/// <summary>
/// FR-5.6, Assumptions §7 — "configurable and defaults to a short window (e.g., 15-30
/// minutes) pending final business decision." Bind from appsettings.json under the
/// "InventoryReservation" section: { "InventoryReservation": { "ExpiryMinutes": 20 } }
/// </summary>
public sealed class InventoryReservationOptions
{
    public const string SectionName = "InventoryReservation";

    public int ExpiryMinutes { get; set; } = 20;

    /// <summary>FR-5.6 — how often InventoryReservationExpiryJob sweeps for reservations past ExpiryMinutes. Independent of ExpiryMinutes itself: a shorter scan interval just tightens how close to the actual expiry moment stock gets released.</summary>
    public int ScanIntervalMinutes { get; set; } = 5;
}
