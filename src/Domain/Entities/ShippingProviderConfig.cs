using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-11.1 to FR-11.3. Stores which concrete IShippingProvider implementations
/// (Local/DHL/Aramex/FedEx) are active and their provider-specific config blob.
/// </summary>
public sealed class ShippingProviderConfig : AggregateRoot<Guid>
{
    public string ProviderName { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public string ConfigJson { get; private set; } = "{}";

    private ShippingProviderConfig() { } // EF Core

    public static ShippingProviderConfig Create(string providerName, bool isActive, string configJson)
    {
        if (string.IsNullOrWhiteSpace(providerName)) throw new DomainException("Provider name is required.");

        return new ShippingProviderConfig
        {
            Id = Guid.NewGuid(),
            ProviderName = providerName,
            IsActive = isActive,
            ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson
        };
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void UpdateConfig(string configJson) => ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
}
