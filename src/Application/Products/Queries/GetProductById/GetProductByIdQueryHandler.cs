using MediatR;
using ShippingSystem.Application.Common;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Products.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Products.Queries.GetProductById;

/// <summary>
/// NFR Caching. Cache-aside with a short TTL, not a long-lived cache: Product.QuantityAvailable
/// changes on every order/cancel/expiry-sweep (ReserveStock/ReleaseStock), none of which go
/// through this cache — they always load the aggregate fresh via IProductRepository directly,
/// so a reservation can never oversell because of a stale cached read here. This cache only
/// ever affects how quickly a BROWSING customer sees the latest stock count, never whether a
/// concurrent purchase succeeds — that correctness still comes entirely from RowVersion.
/// </summary>
public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IProductRepository _products;
    private readonly ICacheService _cache;

    public GetProductByIdQueryHandler(IProductRepository products, ICacheService cache)
    {
        _products = products;
        _cache = cache;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Product(request.ProductId);

        var cached = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var dto = ProductMapping.ToDto(product);
        await _cache.SetAsync(cacheKey, dto, CacheDuration, cancellationToken);

        return dto;
    }
}
