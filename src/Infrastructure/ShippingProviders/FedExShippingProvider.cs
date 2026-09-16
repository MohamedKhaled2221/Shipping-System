using ShippingSystem.Application.Abstractions.Shipping;

namespace ShippingSystem.Infrastructure.ShippingProviders;

/// <summary>
/// FR-11.2, Assumptions §7 — stubbed behind IShippingProvider for future activation.
/// See DHLShippingProvider's doc comment for the full reasoning; identical shape here.
/// </summary>
public sealed class FedExShippingProvider : IShippingProvider
{
    public string ProviderName => "FedEx";

    public Task<ShipmentBookingResult> BookShipmentAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShipmentBookingResult(
            Success: false,
            ProviderReference: null,
            FailureReason: "FedEx integration is not yet implemented (v1 stub per Assumptions §7)."));

    public Task CancelBookingAsync(string providerReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<DateTime> EstimateDeliveryDateAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(DateTime.UtcNow.Date.AddDays(6)); // placeholder SLA — replace with FedEx's real rates/zones API
}
