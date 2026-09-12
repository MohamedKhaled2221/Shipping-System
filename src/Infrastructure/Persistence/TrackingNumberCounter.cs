namespace ShippingSystem.Infrastructure.Persistence;

/// <summary>
/// Backs Services.TrackingNumberGenerator (FR-9.1). One row per year; LastSequence is
/// incremented under RowVersion-guarded optimistic concurrency so concurrent shipment
/// creations across the whole system never hand out the same TRK-YYYY-NNNNNN twice.
/// Deliberately NOT a Domain entity — it's a pure persistence implementation detail of
/// "how do we generate a unique number," not a business concept anyone queries about.
/// </summary>
public sealed class TrackingNumberCounter
{
    public int Year { get; set; }
    public int LastSequence { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
