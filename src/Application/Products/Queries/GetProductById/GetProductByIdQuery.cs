using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;
