using ShippingSystem.Application.Abstractions.Shipping;

namespace ShippingSystem.Infrastructure.ShippingProviders;

/// <summary>
/// FR-11.2, Assumptions §7 — "DHL/Aramex/FedEx implementations are stubbed behind the same
/// interface for future activation." This is intentionally NOT a fake success like
/// LocalShippingProvider: it fails clearly and honestly rather than pretending an
/// integration exists. It's still fully wired into DI and IShippingProviderResolver so that
/// activating DHL later (real HTTP client, auth, request/response mapping to their actual
/// API) means replacing this ONE class's method bodies — nothing in Application, the
/// resolver, or any command handler needs to change.
/// </summary>
public sealed class DHLShippingProvider : IShippingProvider
{
    public string ProviderName => "DHL";

    public Task<ShipmentBookingResult> BookShipmentAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShipmentBookingResult(
            Success: false,
            ProviderReference: null,
            FailureReason: "DHL integration is not yet implemented (v1 stub per Assumptions §7)."));

    public Task CancelBookingAsync(string providerReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask; // never actually booked anything with DHL to cancel

    public Task<DateTime> EstimateDeliveryDateAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(DateTime.UtcNow.Date.AddDays(5)); // placeholder SLA — replace with DHL's real rates/zones API
}
