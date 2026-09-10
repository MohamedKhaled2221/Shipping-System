using FluentValidation;

namespace ShippingSystem.Application.Shipments.Commands.CancelShipment;

public sealed class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
{
    public CancelShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
