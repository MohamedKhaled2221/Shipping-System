using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Common;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Application.DeliveryAgents.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, DeliveryAgentDto>
{
    private readonly IDeliveryAgentRepository _agents;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMyProfileCommandHandler(IDeliveryAgentRepository agents, IUnitOfWork unitOfWork)
    {
        _agents = agents;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeliveryAgentDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var agent = await _agents.GetByIdAsync(request.AgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryAgent), request.AgentId);

        if (!string.Equals(agent.Phone, request.Phone, StringComparison.Ordinal))
        {
            var existing = await _agents.GetByPhoneAsync(request.Phone, cancellationToken);
            if (existing is not null && existing.Id != agent.Id)
                throw new DomainException($"A delivery agent with phone '{request.Phone}' already exists.");
        }

        agent.UpdateProfile(request.Name, request.Phone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DeliveryAgentMapping.ToDto(agent);
    }
}
