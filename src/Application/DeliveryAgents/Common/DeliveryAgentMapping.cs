using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.DeliveryAgents.Common;

internal static class DeliveryAgentMapping
{
    public static DeliveryAgentDto ToDto(DeliveryAgent agent) =>
        new(agent.Id, agent.Name, agent.Phone, agent.IsAvailable, agent.CreatedAt);
}
