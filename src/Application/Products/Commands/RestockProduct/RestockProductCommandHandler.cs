using MediatR;
using ShippingSystem.Application.Common;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Products.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Products.Commands.RestockProduct;

public sealed class RestockProductCommandHandler : IRequestHandler<RestockProductCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public RestockProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<ProductDto> Handle(RestockProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.RestockManually(request.Quantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // NFR Caching — a restock is exactly the kind of change a browsing customer must
        // see immediately, not up to CacheDuration seconds later.
        await _cache.RemoveAsync(CacheKeys.Product(product.Id), cancellationToken);

        return ProductMapping.ToDto(product);
    }
}
