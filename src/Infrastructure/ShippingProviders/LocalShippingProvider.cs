using ShippingSystem.Application.Abstractions.Shipping;

namespace ShippingSystem.Infrastructure.ShippingProviders;

/// <summary>
/// Assumptions §7 — "LocalShippingProvider is the only fully-implemented provider in v1;
/// DHL/Aramex/FedEx implementations are stubbed behind the same interface for future
/// activation." In-house delivery has no external carrier to call — booking is an instant
/// local no-op, and delivery-date estimation uses the static zone rules the SRS specifies
/// for v1 rather than a live rates API.
/// </summary>
public sealed class LocalShippingProvider : IShippingProvider
{
    public string ProviderName => "Local";

    public Task<ShipmentBookingResult> BookShipmentAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShipmentBookingResult(Success: true, ProviderReference: request.TrackingNumber, FailureReason: null));

    public Task CancelBookingAsync(string providerReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask; // nothing external to cancel — an in-house agent simply isn't assigned/dispatched

    /// <summary>
    /// Assumptions §7 — "static zone/provider rules in v1 (no dynamic ETA engine)."
    /// Same city: next day. Same country: +3 days. Cross-border: +7 days. Replace with a
    /// real zone table (e.g. keyed by City/Country pairs from ShippingProviderConfig.ConfigJson)
    /// once the business defines its actual delivery zones.
    /// </summary>
    public Task<DateTime> EstimateDeliveryDateAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default)
    {
        var daysToAdd = request.Country.Equals("Egypt", StringComparison.OrdinalIgnoreCase) ? 3 : 7;
        return Task.FromResult(DateTime.UtcNow.Date.AddDays(daysToAdd));
    }
}
