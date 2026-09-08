namespace ShippingSystem.Application.Abstractions.Shipping;

/// <summary>
/// FR-11.1, FR-11.3. Business/application logic interacts ONLY with this interface —
/// never with LocalShippingProvider/DHLShippingProvider/AramexShippingProvider/
/// FedExShippingProvider directly. Concrete implementations live in Infrastructure
/// (FR-11.2) and are selected at runtime via ShippingProviderConfig.IsActive.
/// Adding a new provider means implementing this interface with zero changes here.
/// </summary>
public interface IShippingProvider
{
    /// <summary>Unique key matching ShippingProviderConfig.ProviderName (e.g. "Local", "DHL").</summary>
    string ProviderName { get; }

    /// <summary>Books the shipment with the external carrier (no-op / instant for LocalShippingProvider).</summary>
    Task<ShipmentBookingResult> BookShipmentAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancels a previously booked shipment with the carrier, where supported.</summary>
    Task CancelBookingAsync(string providerReference, CancellationToken cancellationToken = default);

    /// <summary>Calculates the estimated delivery date using the provider's zone/SLA rules (Assumptions §7 — static in v1).</summary>
    Task<DateTime> EstimateDeliveryDateAsync(ShipmentBookingRequest request, CancellationToken cancellationToken = default);
}

public sealed record ShipmentBookingRequest(
    Guid ShipmentId,
    string TrackingNumber,
    string Country,
    string City,
    string Area,
    string Street,
    string RecipientPhone,
    decimal ShippingFee);

public sealed record ShipmentBookingResult(bool Success, string? ProviderReference, string? FailureReason);
