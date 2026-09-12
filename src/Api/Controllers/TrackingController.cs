using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Tracking.Queries.GetShipmentTracking;

namespace ShippingSystem.Api.Controllers;

/// <summary>
/// FR-9.2. Public/unauthenticated by design — see GetShipmentTrackingQuery's doc comment.
/// The authenticated Admin/Agent/Customer-owner view of a shipment (full aggregate,
/// including OrderId/ShippingFee/etc.) is ShipmentsController.GetById instead; this
/// endpoint intentionally returns only the subset FR-9.2 lists as customer-facing.
/// </summary>
[ApiController]
[Route("api/v1/tracking")]
[AllowAnonymous]
public sealed class TrackingController : ControllerBase
{
    private readonly ISender _sender;
    public TrackingController(ISender sender) => _sender = sender;

    [HttpGet("{trackingNumber}")]
    [ProducesResponseType(typeof(ShipmentTrackingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentTrackingDto>> GetByTrackingNumber(string trackingNumber, CancellationToken cancellationToken)
    {
        var tracking = await _sender.Send(new GetShipmentTrackingQuery(trackingNumber), cancellationToken);
        return Ok(tracking);
    }
}
