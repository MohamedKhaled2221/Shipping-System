using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.DeliveryAgents.Commands.SetMyAvailability;

public sealed class SetMyAvailabilityCommandHandler : IRequestHandler<SetMyAvailabilityCommand, DeliveryAgentDto>
{
    private readonly IDeliveryAgentRepository _agents;
    private readonly IUnitOfWork _unitOfWork;

    public SetMyAvailabilityCommandHandler(IDeliveryAgentRepository agents, IUnitOfWork unitOfWork)
    {
        _agents = agents;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeliveryAgentDto> Handle(SetMyAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var agent = await _agents.GetByIdAsync(request.AgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryAgent), request.AgentId);

        agent.SetAvailability(request.IsAvailable);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DeliveryAgentMapping.ToDto(agent);
    }
}
