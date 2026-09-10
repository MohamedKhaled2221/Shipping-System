using FluentValidation;

namespace ShippingSystem.Application.Orders.Commands.ConfirmOrderPayment;

public sealed class ConfirmOrderPaymentCommandValidator : AbstractValidator<ConfirmOrderPaymentCommand>
{
    public ConfirmOrderPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ProviderName).NotEmpty();
        RuleFor(x => x.ExternalEventId).NotEmpty();
        RuleFor(x => x.TransactionRef).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ShippingFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FailureReason).NotEmpty().When(x => !x.IsSuccessful);
    }
}
