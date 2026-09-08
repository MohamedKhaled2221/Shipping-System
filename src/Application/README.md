# Application Layer — Product Shipping & Delivery Management System

Depends only on `Domain` + MediatR/FluentValidation/logging-abstractions. No EF Core,
ASP.NET, SQL Server, Redis, or SignalR reference here — those are wired in by
Infrastructure against the interfaces defined in this project (SRS §2.3, §9).

## Structure

```
Application/
 ├── Common/
 │    ├── Interfaces/    Repository interfaces (one per aggregate root), IUnitOfWork,
 │    │                  ICurrentUserService, IDateTimeProvider, IInventoryReservationPolicy,
 │    │                  IIdempotencyService
 │    ├── Behaviors/     ValidationBehavior, LoggingBehavior (MediatR pipeline behaviors)
 │    ├── Exceptions/    NotFoundException, ValidationException (Application-level, distinct
 │    │                  from Domain.Exceptions.DomainException)
 │    └── Models/        Shared DTOs (OrderDto, ShipmentTrackingDto, ...)
 ├── Abstractions/
 │    ├── Shipping/      IShippingProvider, IShippingProviderResolver (FR-11.1/11.3)
 │    ├── Payments/      IPaymentGatewayService (external gateway boundary, Assumptions §7)
 │    └── Notifications/ INotificationDispatcher (FR-10.1/10.2)
 ├── Orders/
 │    ├── Commands/
 │    │    ├── CreateOrder/            full vertical slice: Command + Validator + Handler
 │    │    ├── ConfirmOrderPayment/    the payment-webhook handler (see below)
 │    │    └── CancelOrder/            Command + Validator + Handler
 │    └── Queries/GetOrderById/        Query + Handler
 └── DependencyInjection.cs            AddApplication() — registers MediatR, FluentValidation,
                                       and the two pipeline behaviors
```

## Key design decisions

1. **One repository interface per aggregate root only.** Child entities (`OrderItem`,
   `ShipmentStatusHistory`, `FailedDeliveryReason`) are loaded/saved through their parent
   aggregate — there's no `IOrderItemRepository`. `InventoryReservation` gets a repository
   even though it isn't an `AggregateRoot<TId>` in Domain, because it's looked up
   independently by `OrderId` and by the FR-5.6 expiry sweep, not always alongside a
   `Product`.

2. **`IUnitOfWork` only exposes `SaveChangesAsync`.** Repositories don't get their own
   commit method — every command handler calls `SaveChangesAsync` exactly once, at the
   end, so all repository `.Add(...)` calls and in-memory aggregate mutations in that
   handler commit atomically. This is also documented as the point where Infrastructure
   will collect `IDomainEvent`s off every tracked aggregate and publish them after a
   successful commit.

3. **Two idempotency mechanisms, for two different failure modes:**
   - `IProcessedWebhookEventRepository` guards **inbound provider/gateway webhooks**
     (FR-4.7, FR-11.4) — keyed on `(ProviderName, ExternalEventId)`.
   - `IIdempotencyService` guards **client-initiated retries** (NFR: "critical
     order/shipment/payment creation operations must be idempotent") — keyed on a
     client-supplied `Idempotency-Key` header, used by `CreateOrderCommand`.

   `ConfirmOrderPaymentCommandHandler` demonstrates the first; `CreateOrderCommandHandler`
   demonstrates the second.

4. **`ConfirmOrderPaymentCommandHandler` is the module's centerpiece** — it's the one
   handler where FR-3.4, FR-4.3/4.4/4.5, FR-4.7, FR-5.4, FR-9.1 and FR-11.4 all have to
   agree at once:
   - Discards duplicate webhook deliveries before touching anything (FR-4.7/11.4).
   - Calls `Domain.Services.ShipmentPaymentGuard.EnsureOrderCanBeConfirmed(order)` before
     transitioning `Order` to `Confirmed` — this is the cross-aggregate rule that couldn't
     live on either aggregate alone (see the Domain module's README).
   - Consumes `InventoryReservation`s on success, releases them on failure (FR-5.2/5.4).
   - Creates the `Shipment` only once, only after the order is actually shippable (FR-3.4),
     guarded against duplicate creation on a redelivered event.
   - **Deliberately does not call `IShippingProvider` inline.** Booking the shipment with
     the external carrier is an outbound HTTP call; doing it inside the same DB transaction
     as marking a payment "processed" would tie a slow/flaky third party to the webhook's
     success. `Shipment.Create` already raises `ShipmentCreatedEvent` (see Domain) — a
     separate `INotificationHandler` in a later Shipment module reacts to that event and
     calls `IShippingProviderResolver`/`IShippingProvider` with its own retry policy.

5. **`CancelOrderCommandHandler` and the failed-payment path in
   `ConfirmOrderPaymentCommandHandler` both release inventory the same way** (load active
   `Reserved` reservations for the order, `Release()` each one, `ReleaseStock` on the
   matching `Product`) rather than one of them going through a domain-event side channel —
   keeping the two call sites' behavior visibly identical made an earlier draft's
   inconsistency (one path releasing directly, the other assuming a not-yet-written event
   handler would do it) obvious enough to catch and fix here.

6. **`NotFoundException` and `ValidationException` are Application-level, not
   `Domain.Exceptions.DomainException` subclasses.** They represent "the request itself
   was malformed / referenced something that doesn't exist," which is a different failure
   class from a broken business invariant — the API's global exception middleware (next
   module) will map `NotFoundException` → 404, `ValidationException` → 400 with a field-error
   payload, and any `DomainException` → 400/409 depending on subtype.

7. **`Domain.Exceptions.DomainException` was made concrete (not abstract).** Most invariant
   checks are one-off messages (`"Order must contain at least one item."`) that don't need
   a dedicated subclass; `InvalidStateTransitionException`, `InsufficientInventoryException`,
   and `ConcurrencyConflictException` remain their own types only because calling code
   needs to catch them specifically or read structured data off them.

## What's intentionally *not* here yet

- **Shipment/Payment/Inventory/DeliveryAgent/Notification/Webhook feature slices** —
  `ConfirmOrderPayment` covers the piece of the Payment/Shipment story that touches
  `Order`, but standalone commands like "assign shipment to agent," "update shipment
  status," "process a refund," or "handle a DHL tracking webhook" belong to their own
  modules (matching the module list from the Domain hand-off), each following the same
  Command/Validator/Handler shape established here.
- **Auth** — no `RegisterCustomer`/`Login` commands yet; `ICurrentUserService` is defined
  so later handlers can depend on it, but populating it from a JWT is an Infrastructure/API
  concern.
- **FluentValidation package version pin (11.10.0) and MediatR (12.4.1)** are the versions
  assumed available; adjust in `Application.csproj` if the team standardizes on different
  ones — nothing here relies on version-specific behavior beyond the stable
  `IPipelineBehavior`/`AbstractValidator` APIs.

## Suggested next module

**Module 3: Infrastructure layer** — `AppDbContext` (EF Core) with entity configurations
mapping the Domain aggregates (including `RowVersion` → SQL Server `ROWVERSION` columns
and the `ProcessedWebhookEvent` unique index), concrete repository implementations,
`UnitOfWork`, `LocalShippingProvider`, and the DI wiring (`AddInfrastructure(...)`) that
ties it all to the interfaces defined in this Application module.
