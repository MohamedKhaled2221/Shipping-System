using MediatR;

namespace ShippingSystem.Application.Orders.Commands.ConfirmOrderPayment;

/// <summary>
/// FR-4.1 to FR-4.7, FR-3.4. This is the handler an inbound payment-gateway webhook
/// controller calls after verifying the callback's signature. ExternalEventId is the
/// gateway's own event/notification id (NOT the same as TransactionRef, which identifies
/// the charge itself) — it's what FR-4.7/FR-11.4 idempotency is keyed on, since a gateway
/// may redeliver the identical event multiple times.
/// </summary>
public sealed record ConfirmOrderPaymentCommand(
    Guid OrderId,
    string ProviderName,
    string ExternalEventId,
    string TransactionRef,
    decimal Amount,
    bool IsSuccessful,
    decimal ShippingFee,
    string? FailureReason = null) : IRequest;
