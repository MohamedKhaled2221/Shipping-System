using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Orders.Queries.GetOrderById;

/// <summary>FR-2.3 (order history) / general order lookup, used by both Customer and Admin screens.</summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;
