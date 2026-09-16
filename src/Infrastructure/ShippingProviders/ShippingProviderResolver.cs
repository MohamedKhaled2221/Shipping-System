using ShippingSystem.Application.Abstractions.Shipping;

namespace ShippingSystem.Infrastructure.ShippingProviders;

/// <summary>
/// FR-11.2/11.3 — wraps the DI-registered IEnumerable&lt;IShippingProvider&gt; so Application
/// code never takes a direct dependency on the DI container itself. Adding
/// DHLShippingProvider/AramexShippingProvider/FedExShippingProvider later means registering
/// them in DependencyInjection.cs — this class needs no changes.
/// </summary>
public sealed class ShippingProviderResolver : IShippingProviderResolver
{
    private readonly IReadOnlyDictionary<string, IShippingProvider> _providersByName;

    public ShippingProviderResolver(IEnumerable<IShippingProvider> providers) =>
        _providersByName = providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);

    public IShippingProvider Resolve(string providerName)
    {
        if (_providersByName.TryGetValue(providerName, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"No IShippingProvider is registered for provider name '{providerName}'. " +
            $"Registered providers: {string.Join(", ", _providersByName.Keys)}.");
    }
}
