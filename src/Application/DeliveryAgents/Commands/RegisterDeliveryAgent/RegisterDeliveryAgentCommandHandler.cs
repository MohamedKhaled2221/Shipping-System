using MediatR;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Common;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Application.DeliveryAgents.Commands.RegisterDeliveryAgent;

public sealed class RegisterDeliveryAgentCommandHandler : IRequestHandler<RegisterDeliveryAgentCommand, DeliveryAgentDto>
{
    private readonly IDeliveryAgentRepository _agents;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDeliveryAgentCommandHandler(
        IDeliveryAgentRepository agents, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _agents = agents;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeliveryAgentDto> Handle(RegisterDeliveryAgentCommand request, CancellationToken cancellationToken)
    {
        // The unique index on DeliveryAgents.Phone (see Infrastructure) is the real
        // guarantee; this check just gives a friendlier error than a raw DB constraint failure.
        if (await _agents.GetByPhoneAsync(request.Phone, cancellationToken) is not null)
            throw new DomainException($"A delivery agent with phone '{request.Phone}' already exists.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var agent = DeliveryAgent.Register(request.Name, request.Phone, passwordHash);
        _agents.Add(agent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DeliveryAgentMapping.ToDto(agent);
    }
}
