using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShippingSystem.Api.Contracts;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Orders.Queries.GetOrderById;
using ShippingSystem.Application.Shipments.Commands.AssignShipmentToAgent;
using ShippingSystem.Application.Shipments.Commands.CancelShipment;
using ShippingSystem.Application.Shipments.Commands.ReturnShipment;
using ShippingSystem.Application.Shipments.Commands.UpdateShipmentStatus;
using ShippingSystem.Application.Shipments.Queries.GetShipmentById;
using ShippingSystem.Application.Shipments.Queries.GetShipmentsByAgent;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Api.Controllers;

/// <summary>
/// FR-6.2 to FR-6.5, FR-7.2 to FR-7.5. AssignedToCourier, Cancelled, and Returned each get
/// their own endpoint/command (see the Application-layer doc comments) rather than being
/// reachable through the generic status-update endpoint below.
/// </summary>
[ApiController]
[Route("api/v1/shipments")]
[Authorize]
public sealed class ShipmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public ShipmentsController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Admin/shipping-employee and the shipment's own assigned agent may read it; a
    /// Customer may read it only if they own the underlying order.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShipmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await _sender.Send(new GetShipmentByIdQuery(id), cancellationToken);

        if (!await CanAccessAsync(shipment, cancellationToken))
            return Forbid();

        return Ok(shipment);
    }

    /// <summary>FR-7.2, FR-7.4 — the calling agent's own assigned shipments only; there is no "list all" for other agents' data here.</summary>
    [Authorize(Roles = "DeliveryAgent")]
    [HttpGet("agents/me")]
    [ProducesResponseType(typeof(IReadOnlyList<ShipmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShipmentDto>>> GetMyAssignedShipments(CancellationToken cancellationToken)
    {
        var shipments = await _sender.Send(new GetShipmentsByAgentQuery(_currentUser.UserId!.Value), cancellationToken);
        return Ok(shipments);
    }

    /// <summary>FR-6.3 — Admin/shipping-employee only; also advances the shipment to AssignedToCourier (see command doc comment).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShipmentDto>> Assign(Guid id, [FromBody] AssignShipmentRequest request, CancellationToken cancellationToken)
    {
        var command = new AssignShipmentToAgentCommand(id, request.DeliveryAgentId, AssignedBy: $"{_currentUser.Role}:{_currentUser.UserId}");
        var shipment = await _sender.Send(command, cancellationToken);
        return Ok(shipment);
    }

    /// <summary>FR-6.2, FR-7.3, FR-7.5. Admin or the shipment's own assigned agent — an agent may not update a shipment assigned to someone else.</summary>
    [Authorize(Roles = "Admin,DeliveryAgent")]
    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShipmentDto>> UpdateStatus(Guid id, [FromBody] UpdateShipmentStatusRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role == UserRole.DeliveryAgent && !await IsAssignedAgentAsync(id, cancellationToken))
            return Forbid();

        var command = new UpdateShipmentStatusCommand(
            id, request.NewStatus, ChangedBy: $"{_currentUser.Role}:{_currentUser.UserId}", request.Notes);

        var shipment = await _sender.Send(command, cancellationToken);
        return Ok(shipment);
    }

    /// <summary>FR-6.2 — Admin/shipping-employee only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelShipmentRequest request, CancellationToken cancellationToken)
    {
        var command = new CancelShipmentCommand(id, CancelledBy: $"{_currentUser.Role}:{_currentUser.UserId}", request.Reason);
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>FR-6.4. Admin or the shipment's own assigned agent.</summary>
    [Authorize(Roles = "Admin,DeliveryAgent")]
    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Return(Guid id, [FromBody] ReturnShipmentRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role == UserRole.DeliveryAgent && !await IsAssignedAgentAsync(id, cancellationToken))
            return Forbid();

        var command = new ReturnShipmentCommand(id, ReturnedBy: $"{_currentUser.Role}:{_currentUser.UserId}", request.Reason);
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanAccessAsync(ShipmentDto shipment, CancellationToken cancellationToken)
    {
        return _currentUser.Role switch
        {
            UserRole.Admin => true,
            UserRole.DeliveryAgent => shipment.DeliveryAgentId == _currentUser.UserId,
            UserRole.Customer => (await _sender.Send(new GetOrderByIdQuery(shipment.OrderId), cancellationToken)).CustomerId == _currentUser.UserId,
            _ => false
        };
    }

    private async Task<bool> IsAssignedAgentAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await _sender.Send(new GetShipmentByIdQuery(shipmentId), cancellationToken);
        return shipment.DeliveryAgentId == _currentUser.UserId;
    }
}
