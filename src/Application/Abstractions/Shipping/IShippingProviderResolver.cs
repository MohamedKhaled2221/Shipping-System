namespace ShippingSystem.Application.Abstractions.Shipping;

/// <summary>
/// Resolves the correct IShippingProvider implementation for a given provider name
/// (as stored in ShippingProviderConfig). Implemented in Infrastructure by wrapping
/// IEnumerable&lt;IShippingProvider&gt; keyed by ProviderName — keeps Application code
/// from taking a direct dependency on the DI container.
/// </summary>
public interface IShippingProviderResolver
{
    IShippingProvider Resolve(string providerName);
}
