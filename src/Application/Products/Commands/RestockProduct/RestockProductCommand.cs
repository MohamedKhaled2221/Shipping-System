using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Products.Commands.RestockProduct;

/// <summary>
/// FR-5.x manual restock. Uses Product.RestockManually, guarded by the same RowVersion
/// concurrency token as ReserveStock/ReleaseStock (FR-5.3) — a restock racing against a
/// customer's concurrent order-checkout reservation on the same product surfaces as
/// UnitOfWork's ConcurrencyConflictException (HTTP 409) rather than silently losing one
/// of the two updates.
/// </summary>
public sealed record RestockProductCommand(Guid ProductId, int Quantity) : IRequest<ProductDto>;
