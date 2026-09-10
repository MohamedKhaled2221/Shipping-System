using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Products.Commands.UpdateProduct;

/// <summary>FR-3.1 — name/price edits only; stock changes go through RestockProduct instead, so a routine price update can never accidentally clobber a concurrent reservation's stock math.</summary>
public sealed record UpdateProductCommand(Guid ProductId, string Name, decimal Price) : IRequest<ProductDto>;
