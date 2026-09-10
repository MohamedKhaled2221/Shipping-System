using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Services;

namespace ShippingSystem.Application.Orders.Commands.ConfirmOrderPayment;

/// <summary>
/// The single place FR-3.4, FR-4.3, FR-4.4, FR-4.5, FR-4.7, FR-5.4, FR-9.1 and FR-11.4
/// meet. Kept as one handler (one transaction) rather than split across smaller commands,
/// because "mark payment result" and "react to it on the Order/Shipment/Inventory side"
/// must commit atomically — a crash between the two would leave the system in a state
/// the webhook can never safely retry into (the ledger row below is what makes the retry
/// safe in the first place).
///
/// Deliberately does NOT call IShippingProvider here: booking the shipment with the
/// external carrier is an outbound HTTP call, and mixing that inside the same DB
/// transaction as this handler would tie a slow/flaky third party to a payment webhook's
/// success. Instead, ShipmentCreatedEvent (raised by Shipment.Create below) is handled by
/// a separate MediatR INotificationHandler in the Shipment module, which calls
/// IShippingProviderResolver/IShippingProvider on its own with its own retry policy.
/// </summary>
public sealed class ConfirmOrderPaymentCommandHandler : IRequestHandler<ConfirmOrderPaymentCommand>
{
    private readonly IProcessedWebhookEventRepository _processedEvents;
    private readonly IOrderRepository _orders;
    private readonly IPaymentRepository _payments;
    private readonly IInventoryReservationRepository _reservations;
    private readonly IProductRepository _products;
    private readonly IShipmentRepository _shipments;
    private readonly ITrackingNumberGenerator _trackingNumbers;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderPaymentCommandHandler(
        IProcessedWebhookEventRepository processedEvents,
        IOrderRepository orders,
        IPaymentRepository payments,
        IInventoryReservationRepository reservations,
        IProductRepository products,
        IShipmentRepository shipments,
        ITrackingNumberGenerator trackingNumbers,
        IUnitOfWork unitOfWork)
    {
        _processedEvents = processedEvents;
        _orders = orders;
        _payments = payments;
        _reservations = reservations;
        _products = products;
        _shipments = shipments;
        _trackingNumbers = trackingNumbers;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ConfirmOrderPaymentCommand request, CancellationToken cancellationToken)
    {
        // FR-4.7, FR-11.4 — a duplicate webhook delivery is discarded before touching
        // anything else. This check + the unique DB index on (ProviderName, ExternalEventId)
        // together close the race between two concurrent deliveries of the same event.
        if (await _processedEvents.ExistsAsync(request.ProviderName, request.ExternalEventId, cancellationToken))
            return;

        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var payment = await _payments.GetByTransactionRefAsync(request.TransactionRef, cancellationToken);
        if (payment is null)
        {
            payment = Payment.CreatePending(order.Id, request.Amount, order.PaymentMethod, request.TransactionRef);
            _payments.Add(payment);
        }

        if (request.IsSuccessful)
            await HandleSuccessfulPaymentAsync(order, payment, request, cancellationToken);
        else
            await HandleFailedPaymentAsync(order, payment, cancellationToken);

        _processedEvents.Add(Domain.Entities.ProcessedWebhookEvent.Create(request.ProviderName, request.ExternalEventId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSuccessfulPaymentAsync(
        Order order, Payment payment, ConfirmOrderPaymentCommand request, CancellationToken cancellationToken)
    {
        // Payment.MarkPaid() is itself a no-op if already Paid (see Domain.Entities.Payment) —
        // a second line of defense on top of the webhook ledger above.
        payment.MarkPaid();
        order.UpdatePaymentStatus(PaymentStatus.Paid);

        // FR-4.5 (positive case) / general flow — Confirmed only reachable once payment is settled
        // for prepaid orders; COD orders would already have moved to Confirmed earlier in their
        // lifecycle without waiting on this handler at all.
        if (order.Status == OrderStatus.Pending)
        {
            ShipmentPaymentGuard.EnsureOrderCanBeConfirmed(order);
            order.TransitionTo(OrderStatus.Confirmed);
        }

        // FR-5.2/5.4 counterpart: the hold becomes a permanent decrement now that payment cleared.
        var activeReservations = (await _reservations.GetByOrderIdAsync(order.Id, cancellationToken))
            .Where(r => r.Status == ReservationStatus.Reserved)
            .ToList();
        foreach (var reservation in activeReservations)
            reservation.Consume();

        // FR-3.4 — "creates a shipment ... only once the order is in a valid state to ship."
        // Guarded so a redelivered/duplicate-in-a-different-way event can never create a second
        // shipment for the same order (belt-and-suspenders alongside the webhook ledger).
        var existingShipment = await _shipments.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existingShipment is null)
        {
            var trackingNumber = _trackingNumbers.Generate(DateTime.UtcNow.Year);
            var shipment = Shipment.Create(order.Id, trackingNumber, order.ShippingAddressId, request.ShippingFee);
            _shipments.Add(shipment);
        }
    }

    private async Task HandleFailedPaymentAsync(Order order, Payment payment, CancellationToken cancellationToken)
    {
        payment.MarkFailed();
        order.UpdatePaymentStatus(PaymentStatus.Failed);

        // FR-4.5 — "the associated order shall not proceed to Confirmed, and reserved inventory
        // shall be released." A failed prepaid order that hasn't shipped has no reason to stay
        // open, so it moves to Cancelled here (an Application-layer policy choice, not a literal
        // FR line item — the FR only mandates the release + the Confirmed block).
        var activeReservations = (await _reservations.GetByOrderIdAsync(order.Id, cancellationToken))
            .Where(r => r.Status == ReservationStatus.Reserved)
            .ToList();

        if (activeReservations.Count > 0)
        {
            var productIds = activeReservations.Select(r => r.ProductId).Distinct();
            var productsById = (await _products.GetByIdsAsync(productIds, cancellationToken))
                .ToDictionary(p => p.Id);

            foreach (var reservation in activeReservations)
            {
                reservation.Release();
                productsById[reservation.ProductId].ReleaseStock(reservation.QuantityReserved);
            }
        }

        if (order.Status == OrderStatus.Pending)
            order.TransitionTo(OrderStatus.Cancelled);
    }
}
