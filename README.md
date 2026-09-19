# 📦 Product Shipping & Delivery Management System

A production-style **Product Shipping & Delivery Management System** for an e-commerce business, covering the full order lifecycle — order placement, payment, inventory reservation, shipment creation, delivery-agent assignment, real-time tracking, and notifications.

Built with **Clean Architecture** on **.NET 10 / ASP.NET Core Web API**, with an **Angular 18** frontend.

> Backend implements the system described in the project's [Software Requirements Specification (SRS v3.0)](Shipping_System_SRS.md).

---

## ✨ Key Features

- 🔐 **JWT authentication** with refresh tokens for three separate roles — Customer, Admin, Delivery Agent
- 🧾 **Order management** with a strict `Pending → Confirmed → Processing → Completed / Cancelled` state machine
- 📦 **Inventory reservation** with optimistic concurrency control — prevents overselling under concurrent load
- 💳 **Payment tracking** independent of order/shipment status, with idempotent webhook processing
- 🚚 **Shipment management** with its own explicit state machine (`Pending → Confirmed → Preparing → AssignedToCourier → PickedUp → OutForDelivery → Delivered`, plus failure/retry/return/cancel flows)
- 👷 **Delivery agent workflow** — assignment, availability, delivery history, failure reasons
- 🔎 **Public tracking** by tracking number (`TRK-YYYY-NNNNNN`) with full status history
- 🔌 **Pluggable shipping providers** (`LocalShippingProvider` implemented; DHL/Aramex/FedEx stubbed) behind a single `IShippingProvider` abstraction
- 📡 **Real-time updates** via SignalR
- ♻️ **Idempotent webhooks** for payment and shipping-provider callbacks
- 🖥️ **Angular 18** standalone-components frontend wired to the real API contracts

---

## 🏗️ Architecture

Clean Architecture with strict layer separation and one-way dependencies:

```
src/
 ├── Domain/          Entities, Enums, OrderStateMachine, ShipmentStateMachine, Domain Events
 ├── Application/      CQRS Commands/Queries (MediatR), IShippingProvider, IPaymentService, DTOs, FluentValidation
 ├── Infrastructure/    EF Core, Redis, SignalR Hub, Shipping provider clients, Payment gateway client, Notifications
 └── Api/              Controllers, Middleware, DI composition root, Swagger

frontend/
 └── src/app/          Angular 18 standalone components, signals, functional guards/interceptors
```

`Api` is the only project that wires everything together (`AddApplication()` + `AddInfrastructure()`) and the only one aware of HTTP/JWT/Swagger — `Domain` has no dependency on any other layer.

### Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, C# |
| Data | Entity Framework Core, SQL Server |
| Caching | Redis |
| Real-time | SignalR |
| Auth | JWT access tokens + refresh tokens |
| Frontend | Angular 18 (standalone components, signals) |
| Patterns | Clean Architecture, CQRS, MediatR, FluentValidation, Optimistic Concurrency, Idempotency |

---

## 🔄 State Machines

Order status and shipment status are **deliberately separate state machines** — a shipment's status never overwrites or is inferred from the order's status.

**Order Status**
```
Pending → Confirmed → Processing → Completed
Any non-terminal state → Cancelled
```

**Shipment Status**
```
Pending → Confirmed → Preparing → AssignedToCourier → PickedUp → OutForDelivery → Delivered

OutForDelivery → Failed
Failed → OutForDelivery   (retry)
Failed → Returned
Any non-terminal state → Cancelled
```

Any transition not listed is rejected by the domain layer.

---

## 🔌 API Overview

Base path: `/api/v1`. Full interactive documentation is served via Swagger at `/swagger` when running the API.

| Controller | Route prefix | Purpose |
|---|---|---|
| `AuthController` | `/auth` | Customer/Admin/Agent register & login, token refresh |
| `ProductsController` | `/products` | Product catalog, stock, restock |
| `OrdersController` | `/orders` | Place order, view, cancel |
| `ShipmentsController` | `/shipments` | View, assign, update status, cancel, return |
| `DeliveryAgentsController` | `/delivery-agents` | Agent registration, profile, availability |
| `TrackingController` | `/tracking` | Public tracking-number lookup |
| `PaymentsWebhookController` | `/webhooks/payments` | Idempotent payment-status callbacks |

Authenticated endpoints require `Authorization: Bearer <token>`.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or container)
- Redis (local or container)
- Node.js 18+ and npm (for the Angular frontend)

### Backend

```bash
# from the repository root
cd src/Api

# configure connection strings / JWT secret in appsettings.json or
# via environment variables / user-secrets before running

dotnet restore
dotnet ef database update       # applies EF Core migrations
dotnet run
```

The API starts on the port shown in the console and serves Swagger UI at `/swagger`. On first run against an empty database, seed data is created automatically:

| Role | Login | Password |
|---|---|---|
| Admin | `admin@shippingsystem.com` | `Admin@12345` |
| Delivery Agent | `01000000001` | `Agent@12345` |
| Customer | `customer@shippingsystem.com` | `Customer@12345` |

Key configuration (`src/Api/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=ShippingSystemDb;...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "REPLACE_WITH_A_RANDOM_SECRET_AT_LEAST_32_CHARACTERS_LONG",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 14
  },
  "InventoryReservation": {
    "ExpiryMinutes": 20,
    "ScanIntervalMinutes": 5
  }
}
```

> ⚠️ Replace the JWT secret before deploying anywhere beyond your own machine.

### Frontend

```bash
cd frontend
npm install
npm start       # ng serve → http://localhost:4200
```

Point the app at your API by editing `src/environments/environment.ts`. CORS is pre-configured on the API for `http://localhost:4200`.

---

## 🧱 Solution Structure

```
ShippingSystem.sln
├── src/Domain            Domain.csproj
├── src/Application       Application.csproj
├── src/Infrastructure    Infrastructure.csproj
├── src/Api               Api.csproj  (entry point)
└── frontend               Angular workspace
```

Open `ShippingSystem.sln` in Visual Studio / Rider, or work from the CLI with `dotnet build` / `dotnet test` from the repository root.

---

## 🛡️ Concurrency & Idempotency

- Inventory reservation, shipment assignment, and shipment status updates all use **optimistic concurrency control** (row version) to stay correct under concurrent load.
- Payment webhooks and shipping-provider status webhooks are processed **idempotently**, using a stored event ID (`ProcessedWebhookEvent`) to detect and discard duplicates.

---

## 📄 License

No license has been specified yet for this repository. Add a `LICENSE` file to define how others may use this code.
