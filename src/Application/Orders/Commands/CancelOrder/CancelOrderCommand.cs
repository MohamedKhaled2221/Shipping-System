using MediatR;

namespace ShippingSystem.Application.Orders.Commands.CancelOrder;

/// <summary>
/// SRS §2.2 step 11, FR-5.4. CancelledBy is the acting user's display name/id (Customer or
/// Admin) recorded for audit purposes — actual authorization (can THIS user cancel THIS
/// order?) is enforced by the API layer's [Authorize] policies, not here.
/// </summary>
public sealed record CancelOrderCommand(Guid OrderId, string CancelledBy) : IRequest;
