# Domain Layer — Product Shipping & Delivery Management System

Pure C# class library. **Zero dependencies** on EF Core, ASP.NET, MediatR, or any
Infrastructure concern — this is intentional per Clean Architecture (SRS §2.3, §9).

## Structure

```
Domain/
 ├── Common/          Entity<TId>, AggregateRoot<TId>, IDomainEvent, IHasConcurrencyToken
 ├── Enums/           OrderStatus, PaymentStatus, ShipmentStatus, ReservationStatus,
 │                    PaymentMethod, UserRole, Notification* enums
 ├── Entities/        Customer, ShippingAddress, Product, InventoryReservation,
 │                    Order, OrderItem, Payment, Shipment, ShipmentStatusHistory,
 │                    FailedDeliveryReason, DeliveryAgent, Notification,
 │                    ShippingProviderConfig, ProcessedWebhookEvent
 ├── Exceptions/      DomainException, InvalidStateTransitionException,
 │                    InsufficientInventoryException, ConcurrencyConflictException
 ├── StateMachines/   OrderStateMachine, ShipmentStateMachine (kept fully separate — SRS §2.3)
 ├── Events/          OrderEvents, ShipmentEvents, InventoryEvents
 └── Services/        ShipmentPaymentGuard, ITrackingNumberGenerator
```

## Key design decisions

1. **Aggregate boundaries.** `Order` and `Shipment` are separate aggregate roots with
   separate consistency boundaries. Neither holds a live reference to the other —
   only `OrderId`/no back-reference. This matches SRS §2.3's hard requirement that
   Order Status and Shipment Status "must never be conflated."

2. **State machines as pure lookup tables**, not enum methods on the entities
   themselves. `OrderStateMachine.CanTransition` / `ShipmentStateMachine.CanTransition`
   are the single source of truth (FR-3.5, FR-6.7). Both `Order.TransitionTo` and
   `Shipment.TransitionTo` delegate to them and throw `InvalidStateTransitionException`
   on any transition not in the graph.

3. **Cross-aggregate rules live in `Domain.Services`, not on either aggregate.**
   FR-4.3 ("prepaid shipment can't progress past Pending/Confirmed unless Paid") and
   FR-4.5 ("failed payment blocks Confirmed") need both `Order` and `Shipment` (or
   just `Order`) in scope at once. `ShipmentPaymentGuard.EnsureCanProgress(order,
   targetStatus)` must be called by the Application layer immediately before
   `shipment.TransitionTo(targetStatus, ...)`. This keeps each aggregate's own
   invariants local while still enforcing the cross-cutting rule.

4. **Optimistic concurrency (`IHasConcurrencyToken`)** is implemented by `Product`,
   `Order`, and `Shipment` — the three aggregates the SRS explicitly calls out
   (FR-5.3, FR-6.3, FR-6.5). `RowVersion` is a plain `byte[]`; EF Core will map it
   to a SQL Server `ROWVERSION` column in the Infrastructure layer. The Domain
   never throws `DbUpdateConcurrencyException` — the Application layer catches
   that EF Core–specific exception and translates it into the Domain's own
   `ConcurrencyConflictException`.

5. **Idempotency scaffolding.** `ProcessedWebhookEvent` is the ledger row Application
   checks before processing any provider/payment webhook (FR-11.4, FR-4.7).
   `Payment.MarkPaid()` / `MarkFailed()` are themselves idempotent no-ops if called
   twice with the same already-reached status, as a second line of defense.

6. **`InventoryReservation` is a plain entity, not an aggregate root.** It's always
   created/mutated together with its `Product` inside a single application-layer
   transaction, so it doesn't need its own repository or concurrency token —
   `Product.RowVersion` is what actually protects against oversell (FR-5.3, NFR).

7. **Domain events are collected, not dispatched, by entities.** Each aggregate calls
   `AddDomainEvent(...)`; the Application layer (via a MediatR `IPublisher` or an
   EF Core `SaveChanges` interceptor) reads `entity.DomainEvents`, dispatches them
   *after* a successful commit, then calls `entity.ClearDomainEvents()`. This keeps
   the Domain persistence-and-messaging agnostic.

8. **`ITrackingNumberGenerator`** is declared here but implemented in Infrastructure
   (backed by a SQL sequence or Redis `INCR`), so `TRK-YYYY-NNNNNN` generation
   (FR-9.1) stays testable and swappable without touching the Domain.

## What's intentionally *not* here yet

- Repository interfaces (`IOrderRepository`, etc.) — these belong in `Application`,
  since Application owns the use-case-shaped contracts the Domain shouldn't know about.
- `IShippingProvider` — belongs in `Application` per FR-11.1/11.3 ("business/application
  logic interacts only with `IShippingProvider`").
- Validation via FluentValidation — that's input/DTO validation in `Application`, distinct
  from the domain invariants enforced here via constructors/factory methods and exceptions.

## Suggested next module

**Module 2: Application layer skeleton** — repository interfaces, `IShippingProvider`,
`IPaymentService`, `IUnitOfWork`, and the first vertical slice (Create Order command)
wired through `ShipmentPaymentGuard` and the two state machines above.
