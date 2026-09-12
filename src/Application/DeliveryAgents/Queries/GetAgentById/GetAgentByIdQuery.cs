using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.DeliveryAgents.Queries.GetAgentById;

/// <summary>Admin lookup of a specific agent's profile — e.g. before assigning a shipment (FR-6.3).</summary>
public sealed record GetAgentByIdQuery(Guid AgentId) : IRequest<DeliveryAgentDto>;
