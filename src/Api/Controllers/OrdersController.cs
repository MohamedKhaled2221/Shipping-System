using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShippingSystem.Api.Contracts;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Orders.Commands.CancelOrder;
using ShippingSystem.Application.Orders.Commands.CreateOrder;
using ShippingSystem.Application.Orders.Queries.GetOrderById;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>
    /// FR-3.2, FR-3.4. The Idempotency-Key header (NFR: "critical order/shipment creation
    /// operations must be idempotent") is required, not optional — a checkout button that
    /// can be double-tapped without one is exactly the bug this header exists to prevent.
    /// Missing/empty values fail CreateOrderCommandValidator's NotEmpty rule and surface as
    /// a normal 400 through the global exception handler, so there's no separate manual
    /// check needed here.
    /// </summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        // CustomerId comes from the JWT, never the request body — see CreateOrderRequest's
        // own doc comment for why.
        var customerId = _currentUser.UserId!.Value;

        var command = new CreateOrderCommand(
            customerId,
            request.ShippingAddressId,
            request.PaymentMethod,
            request.Items.Select(i => new CreateOrderItemRequest(i.ProductId, i.Quantity)).ToList(),
            idempotencyKey);

        var order = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        // FR-2.3 — a Customer may only view their own orders; Admin has full oversight.
        // DeliveryAgent has no business reason to read an Order directly (their view is
        // scoped to assigned Shipments, a later module), so they're excluded here too.
        if (_currentUser.Role == UserRole.Customer && order.CustomerId != _currentUser.UserId)
            return Forbid();

        return Ok(order);
    }

    [Authorize(Roles = "Customer,Admin")]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.Role == UserRole.Customer)
        {
            var order = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);
            if (order.CustomerId != _currentUser.UserId)
                return Forbid();
        }

        await _sender.Send(new CancelOrderCommand(id, CancelledBy: $"{_currentUser.Role}:{_currentUser.UserId}"), cancellationToken);
        return NoContent();
    }
}
