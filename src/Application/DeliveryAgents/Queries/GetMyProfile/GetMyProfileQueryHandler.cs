using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.DeliveryAgents.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, DeliveryAgentDto>
{
    private readonly IDeliveryAgentRepository _agents;

    public GetMyProfileQueryHandler(IDeliveryAgentRepository agents) => _agents = agents;

    public async Task<DeliveryAgentDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var agent = await _agents.GetByIdAsync(request.AgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryAgent), request.AgentId);

        return DeliveryAgentMapping.ToDto(agent);
    }
}
