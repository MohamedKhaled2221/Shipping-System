using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShippingSystem.Api.Contracts;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Commands.RegisterDeliveryAgent;
using ShippingSystem.Application.DeliveryAgents.Commands.SetMyAvailability;
using ShippingSystem.Application.DeliveryAgents.Commands.UpdateMyProfile;
using ShippingSystem.Application.DeliveryAgents.Queries.GetAgentById;
using ShippingSystem.Application.DeliveryAgents.Queries.GetAvailableAgents;
using ShippingSystem.Application.DeliveryAgents.Queries.GetMyProfile;

namespace ShippingSystem.Api.Controllers;

/// <summary>
/// FR-7.1. FR-7.2/7.4 ("view assigned shipments"/"delivery history") and FR-7.3/7.5
/// ("update delivery status"/"record failure reason") live on ShipmentsController instead —
/// they act on a Shipment, not on the agent's own profile, which is this controller's scope.
/// </summary>
[ApiController]
[Route("api/v1/delivery-agents")]
[Authorize]
public sealed class DeliveryAgentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public DeliveryAgentsController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>Admin onboards a new agent account; the agent authenticates separately via POST /api/v1/auth/agents/login.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryAgentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DeliveryAgentDto>> Register([FromBody] RegisterDeliveryAgentRequest request, CancellationToken cancellationToken)
    {
        var agent = await _sender.Send(new RegisterDeliveryAgentCommand(request.Name, request.Phone, request.Password), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = agent.Id }, agent);
    }

    /// <summary>FR-6.3 support — Admin's pool of agents to assign from.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<DeliveryAgentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeliveryAgentDto>>> GetAvailable(CancellationToken cancellationToken)
    {
        var agents = await _sender.Send(new GetAvailableAgentsQuery(), cancellationToken);
        return Ok(agents);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeliveryAgentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAgentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var agent = await _sender.Send(new GetAgentByIdQuery(id), cancellationToken);
        return Ok(agent);
    }

    /// <summary>FR-7.1 — an agent viewing their own profile.</summary>
    [Authorize(Roles = "DeliveryAgent")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(DeliveryAgentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAgentDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var agent = await _sender.Send(new GetMyProfileQuery(_currentUser.UserId!.Value), cancellationToken);
        return Ok(agent);
    }

    /// <summary>FR-7.1 — an agent updating their own contact info. AgentId is taken from the JWT, never the request body.</summary>
    [Authorize(Roles = "DeliveryAgent")]
    [HttpPut("me")]
    [ProducesResponseType(typeof(DeliveryAgentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAgentDto>> UpdateMyProfile([FromBody] UpdateAgentProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMyProfileCommand(_currentUser.UserId!.Value, request.Name, request.Phone);
        var agent = await _sender.Send(command, cancellationToken);
        return Ok(agent);
    }

    /// <summary>FR-7.1 — an agent going available/unavailable (e.g. clocking in/out).</summary>
    [Authorize(Roles = "DeliveryAgent")]
    [HttpPut("me/availability")]
    [ProducesResponseType(typeof(DeliveryAgentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAgentDto>> SetMyAvailability([FromBody] SetAgentAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var command = new SetMyAvailabilityCommand(_currentUser.UserId!.Value, request.IsAvailable);
        var agent = await _sender.Send(command, cancellationToken);
        return Ok(agent);
    }
}
