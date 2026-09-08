using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Abstractions.Payments;

/// <summary>
/// Assumptions §7 — "Payment processing itself (gateway integration) is treated as an
/// external dependency; the system records PaymentStatus and reacts to it." This is that
/// boundary. Application logic never talks to Stripe/PayMob/etc. directly.
/// </summary>
public interface IPaymentGatewayService
{
    Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);

    /// <summary>FR-4.6 — full or partial refund against a previously captured charge.</summary>
    Task<PaymentRefundResult> RefundAsync(string transactionRef, decimal amount, string reason, CancellationToken cancellationToken = default);
}

public sealed record PaymentChargeRequest(Guid OrderId, decimal Amount, PaymentMethod Method, string IdempotencyKey);

public sealed record PaymentChargeResult(bool Success, string TransactionRef, PaymentStatus ResultingStatus, string? FailureReason);

public sealed record PaymentRefundResult(bool Success, string RefundReference, string? FailureReason);
