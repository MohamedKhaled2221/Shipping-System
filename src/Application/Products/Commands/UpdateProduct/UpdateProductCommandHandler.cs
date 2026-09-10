using MediatR;
using ShippingSystem.Application.Common;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Products.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.UpdateDetails(request.Name, request.Price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // NFR Caching — evict AFTER the commit succeeds, never before: if SaveChangesAsync
        // threw (e.g. a concurrency conflict), the old cached value is still the correct one.
        await _cache.RemoveAsync(CacheKeys.Product(product.Id), cancellationToken);

        return ProductMapping.ToDto(product);
    }
}
