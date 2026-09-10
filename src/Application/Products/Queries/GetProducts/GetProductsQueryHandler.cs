using MediatR;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Products.Common;

namespace ShippingSystem.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _products;

    public GetProductsQueryHandler(IProductRepository products) => _products = products;

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _products.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
        return new PagedResult<ProductDto>(items.Select(ProductMapping.ToDto).ToList(), request.PageNumber, request.PageSize, totalCount);
    }
}
