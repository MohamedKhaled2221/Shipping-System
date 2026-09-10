using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Products.Queries.GetProducts;

/// <summary>FR-3.1 catalog browsing. Public — see ProductsController for the [AllowAnonymous] rationale.</summary>
public sealed record GetProductsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
