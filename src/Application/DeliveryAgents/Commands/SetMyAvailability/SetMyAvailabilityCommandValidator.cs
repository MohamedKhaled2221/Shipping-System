using FluentValidation;

namespace ShippingSystem.Application.DeliveryAgents.Commands.SetMyAvailability;

public sealed class SetMyAvailabilityCommandValidator : AbstractValidator<SetMyAvailabilityCommand>
{
    public SetMyAvailabilityCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
