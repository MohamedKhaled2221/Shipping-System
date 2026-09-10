namespace ShippingSystem.Api.Contracts;

/// <summary>
/// Shape of the inbound payment-gateway callback. A real gateway integration (see
/// Infrastructure's SimulatedPaymentGatewayService docs) would define its own exact payload
/// per provider — this is a generic shape the PaymentsWebhookController maps into
/// ConfirmOrderPaymentCommand regardless of which real gateway eventually sends it.
/// </summary>
public sealed record PaymentWebhookRequest(
    Guid OrderId,
    string ProviderName,
    string ExternalEventId,
    string TransactionRef,
    decimal Amount,
    bool IsSuccessful,
    decimal ShippingFee,
    string? FailureReason);
