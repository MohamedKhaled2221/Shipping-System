using FluentValidation;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Shipments.Commands.UpdateShipmentStatus;

public sealed class UpdateShipmentStatusCommandValidator : AbstractValidator<UpdateShipmentStatusCommand>
{
    // Cancelled/Returned/AssignedToCourier each have their own dedicated command
    // (CancelShipment, ReturnShipment, AssignShipmentToAgent) because they carry extra
    // business behaviour (agent assignment, a mandatory reason) beyond a plain status
    // write — this generic command only covers the plain progression + retry steps.
    private static readonly ShipmentStatus[] AllowedTargets =
    {
        ShipmentStatus.Confirmed,
        ShipmentStatus.Preparing,
        ShipmentStatus.PickedUp,
        ShipmentStatus.OutForDelivery,
        ShipmentStatus.Delivered,
        ShipmentStatus.Failed
    };

    public UpdateShipmentStatusCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.ChangedBy).NotEmpty().MaximumLength(200);

        RuleFor(x => x.NewStatus)
            .Must(status => AllowedTargets.Contains(status))
            .WithMessage(
                "Use the Assign, Cancel, or Return endpoints for AssignedToCourier, Cancelled, or Returned respectively.");

        // FR-7.5 — a failure reason is mandatory, not an optional note, when recording a failed delivery.
        RuleFor(x => x.Notes)
            .NotEmpty()
            .WithMessage("A reason is required when marking a shipment as Failed.")
            .When(x => x.NewStatus == ShipmentStatus.Failed);

        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
