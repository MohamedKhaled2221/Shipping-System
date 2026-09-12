using FluentValidation;

namespace ShippingSystem.Application.Tracking.Queries.GetShipmentTracking;

public sealed class GetShipmentTrackingQueryValidator : AbstractValidator<GetShipmentTrackingQuery>
{
    // FR-9.1's own format, TRK-YYYY-NNNNNN — rejecting an obviously malformed value here
    // (empty, wrong shape) means a typo surfaces as a normal 400, not a wasted DB round
    // trip that returns 404 either way.
    public GetShipmentTrackingQueryValidator()
    {
        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .Matches(@"^TRK-\d{4}-\d{6}$")
            .WithMessage("Tracking number must be in the format TRK-YYYY-NNNNNN.");
    }
}
