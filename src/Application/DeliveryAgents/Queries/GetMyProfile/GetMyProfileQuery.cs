using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.DeliveryAgents.Queries.GetMyProfile;

public sealed record GetMyProfileQuery(Guid AgentId) : IRequest<DeliveryAgentDto>;
