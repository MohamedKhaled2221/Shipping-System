using Microsoft.Extensions.Options;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Infrastructure.Options;

namespace ShippingSystem.Infrastructure.Services;

public sealed class InventoryReservationPolicy : IInventoryReservationPolicy
{
    private readonly InventoryReservationOptions _options;

    public InventoryReservationPolicy(IOptions<InventoryReservationOptions> options) =>
        _options = options.Value;

    public TimeSpan ExpiryWindow => TimeSpan.FromMinutes(_options.ExpiryMinutes);
}
