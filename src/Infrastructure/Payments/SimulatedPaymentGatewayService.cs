using ShippingSystem.Application.Abstractions.Payments;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Infrastructure.Payments;

/// <summary>
/// Assumptions §7 — "Payment processing itself (gateway integration) is treated as an
/// external dependency; the system records PaymentStatus and reacts to it." This is a
/// placeholder so the solution compiles and is demoable end-to-end without a live gateway
/// account. Swap for a real SDK-backed implementation (Stripe/PayMob/Fawry/etc.) before
/// this goes anywhere near production — nothing in Application or Domain needs to change
/// when that happens, only the DI registration in this project's DependencyInjection.cs.
///
/// Deliberately returns PaymentStatus.Pending, not Paid: a real gateway's synchronous
/// charge-API response usually only confirms the charge was ACCEPTED for processing, not
/// that funds actually moved — final settlement arrives later via its own webhook, which is
/// exactly what ConfirmOrderPaymentCommand exists to receive. Mirroring that shape here
/// keeps this stub honest about what a real integration would actually guarantee at this call site.
/// </summary>
public sealed class SimulatedPaymentGatewayService : IPaymentGatewayService
{
    public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        var transactionRef = $"SIM-{request.IdempotencyKey}";
        return Task.FromResult(new PaymentChargeResult(
            Success: true,
            TransactionRef: transactionRef,
            ResultingStatus: PaymentStatus.Pending,
            FailureReason: null));
    }

    public Task<PaymentRefundResult> RefundAsync(string transactionRef, decimal amount, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentRefundResult(
            Success: true,
            RefundReference: $"SIM-REFUND-{Guid.NewGuid():N}",
            FailureReason: null));
}
