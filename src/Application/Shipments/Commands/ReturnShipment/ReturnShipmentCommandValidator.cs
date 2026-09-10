using FluentValidation;

namespace ShippingSystem.Application.Shipments.Commands.ReturnShipment;

public sealed class ReturnShipmentCommandValidator : AbstractValidator<ReturnShipmentCommand>
{
    public ReturnShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.ReturnedBy).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
