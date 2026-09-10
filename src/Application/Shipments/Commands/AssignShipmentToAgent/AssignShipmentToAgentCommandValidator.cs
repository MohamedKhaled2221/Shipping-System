using FluentValidation;

namespace ShippingSystem.Application.Shipments.Commands.AssignShipmentToAgent;

public sealed class AssignShipmentToAgentCommandValidator : AbstractValidator<AssignShipmentToAgentCommand>
{
    public AssignShipmentToAgentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.DeliveryAgentId).NotEmpty();
        RuleFor(x => x.AssignedBy).NotEmpty().MaximumLength(200);
    }
}
