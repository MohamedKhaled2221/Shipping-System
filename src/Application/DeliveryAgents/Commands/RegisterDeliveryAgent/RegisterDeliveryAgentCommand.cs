using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.DeliveryAgents.Commands.RegisterDeliveryAgent;

/// <summary>
/// FR-7.1. Deliberately does NOT auto-issue tokens the way RegisterCustomerCommand does —
/// this is an Admin onboarding an agent's account, not the agent self-registering, so the
/// caller (Admin) has no business receiving the new agent's credentials session. The agent
/// authenticates separately afterwards via POST /api/v1/auth/agents/login.
/// </summary>
public sealed record RegisterDeliveryAgentCommand(string Name, string Phone, string Password) : IRequest<DeliveryAgentDto>;
