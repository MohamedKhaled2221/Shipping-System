using FluentValidation;

namespace ShippingSystem.Application.DeliveryAgents.Commands.RegisterDeliveryAgent;

public sealed class RegisterDeliveryAgentCommandValidator : AbstractValidator<RegisterDeliveryAgentCommand>
{
    public RegisterDeliveryAgentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
    }
}
