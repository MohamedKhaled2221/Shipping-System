using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.DeliveryAgents.Commands.UpdateMyProfile;

/// <summary>FR-7.1 — an agent editing their own contact info. AgentId comes from the JWT in the controller, never the request body.</summary>
public sealed record UpdateMyProfileCommand(Guid AgentId, string Name, string Phone) : IRequest<DeliveryAgentDto>;
