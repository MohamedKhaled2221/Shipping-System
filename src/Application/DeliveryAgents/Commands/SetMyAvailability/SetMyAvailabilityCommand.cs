using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.DeliveryAgents.Commands.SetMyAvailability;

/// <summary>FR-7.1 — toggled by the agent themselves (e.g. going off-shift); also read by Admin's assignment workflow (FR-6.3) via GetAvailableAgentsQuery.</summary>
public sealed record SetMyAvailabilityCommand(Guid AgentId, bool IsAvailable) : IRequest<DeliveryAgentDto>;
