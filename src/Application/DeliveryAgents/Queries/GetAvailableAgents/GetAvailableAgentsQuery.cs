using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.DeliveryAgents.Queries.GetAvailableAgents;

/// <summary>Backs Admin's "pick an agent to assign" screen for FR-6.3.</summary>
public sealed record GetAvailableAgentsQuery : IRequest<IReadOnlyList<DeliveryAgentDto>>;
