namespace ShippingSystem.Application.Common;

/// <summary>
/// Every cache key used anywhere in the solution is built here, once, so a handler that
/// writes a key and a different handler that invalidates it (e.g. GetProductByIdQuery vs.
/// UpdateProductCommand) can never drift into two subtly different string formats. All keys
/// are namespaced under "shippingsystem:" in case the target Redis instance is ever shared
/// with another application.
/// </summary>
public static class CacheKeys
{
    public static string Product(Guid productId) => $"shippingsystem:product:{productId}";

    public static string ShipmentTracking(string trackingNumber) =>
        $"shippingsystem:shipment-tracking:{trackingNumber.ToUpperInvariant()}";

    /// <summary>
    /// Read on every single shipment booking (ShipmentProviderBookingEventHandler) but
    /// written only through a future admin "configure shipping provider" flow — about as
    /// high read-to-write ratio as caching gets in this system.
    /// </summary>
    public const string ActiveShippingProviderName = "shippingsystem:shipping-provider:active-name";
}
