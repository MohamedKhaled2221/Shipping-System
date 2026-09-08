using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-4.1 to FR-4.7. One Order can have multiple Payment records over time
/// (e.g. an initial charge plus a later refund record). TransactionRef is the
/// idempotency key used by the webhook handler (FR-4.7) — enforce uniqueness at the DB level.
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string TransactionRef { get; private set; } = default!;
    public DateTime? ProcessedAt { get; private set; }
    public string? RefundReason { get; private set; }

    private Payment() { } // EF Core

    public static Payment CreatePending(Guid orderId, decimal amount, PaymentMethod method, string transactionRef)
    {
        if (amount <= 0) throw new DomainException("Payment amount must be positive.");
        if (string.IsNullOrWhiteSpace(transactionRef)) throw new DomainException("Transaction reference is required for idempotency.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Method = method,
            TransactionRef = transactionRef,
            PaymentStatus = PaymentStatus.Pending
        };
    }

    /// <summary>FR-4.7 — safe to call more than once with the same outcome; no double-processing.</summary>
    public void MarkPaid()
    {
        if (PaymentStatus == PaymentStatus.Paid) return;
        if (PaymentStatus is PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded)
            throw new DomainException($"Payment '{Id}' is already '{PaymentStatus}' and cannot be marked Paid.");

        PaymentStatus = PaymentStatus.Paid;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException($"Payment '{Id}' is already Paid and cannot be marked Failed.");

        PaymentStatus = PaymentStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>FR-4.6 — records a full or partial refund with reason and timestamp.</summary>
    public void Refund(decimal refundAmount, string reason)
    {
        if (PaymentStatus != PaymentStatus.Paid)
            throw new DomainException($"Only a Paid payment can be refunded; current status is '{PaymentStatus}'.");
        if (refundAmount <= 0 || refundAmount > Amount)
            throw new DomainException("Refund amount must be positive and cannot exceed the original amount.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Refund reason is required.");

        PaymentStatus = refundAmount == Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        RefundReason = reason;
        ProcessedAt = DateTime.UtcNow;
    }
}
