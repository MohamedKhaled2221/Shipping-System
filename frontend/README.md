# Shipping & Delivery Management — Angular Frontend Template

Angular 18 (standalone components, signals, functional guards/interceptors) frontend for the
Product Shipping & Delivery Management System described in the SRS, wired against the actual
ASP.NET Core Web API in `Shipping-System-master` (Clean Architecture, CQRS, JWT auth).

This is a **template/scaffold**: the architecture, routing, auth, and API clients are complete
and match the backend's real controllers/DTOs field-for-field, but visual polish and edge-case
UX are intentionally kept simple so you can extend them.

## Getting started

```bash
npm install
npm start        # ng serve, defaults to http://localhost:4200
```

Point the app at your API by editing `src/environments/environment.ts`:

```ts
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7001/api/v1', // match your backend's launchSettings.json port
};
```

Because the backend uses a self-signed dev HTTPS cert, run `dotnet dev-certs https --trust`
once on your machine, or `ng serve` will fail to reach the API from the browser.

## Structure

```
src/app/
 ├── core/
 │   ├── models/        DTOs and enums — mirror Api/Contracts + Application/Common/Models exactly
 │   ├── services/       One HttpClient wrapper per backend controller, plus CartService,
 │   │                    OrderHistoryService and RecentLookupService (see "Known gaps" below)
 │   ├── interceptors/    Bearer-token attach + silent refresh-on-401; global error → toast
 │   └── guards/          authGuard (must be signed in), roleGuard (role allow-list via route data)
 ├── shared/              Status badge/timeline/pagination components, toast stack, notification
 │                        service, enum → label/tone mapping
 ├── layout/shell/        Top bar + role-aware nav + router-outlet
 └── features/
     ├── home/            Landing page
     ├── auth/             Login (role switcher: customer/admin/agent), customer self-register
     ├── products/         Public catalog; Customer add-to-order; Admin create/edit/restock
     ├── orders/           Checkout (cart → CreateOrderRequest), order list, order detail
     ├── tracking/         Public tracking-number lookup (FR-9.2)
     ├── admin/            Shipment list/detail (assign/status/cancel/return), agent onboarding
     └── agent/            "My deliveries" list, own profile + availability toggle
```

Enum values in `core/models/enums.ts` are numeric and must stay in lockstep with
`src/Domain/Enums/*.cs` — the backend serializes C# enums as integers by default.

## Auth model

Three separate login endpoints (`/auth/customers/login`, `/auth/admins/login`,
`/auth/agents/login`) map to three roles. `AuthService` decodes the JWT to derive the signed-in
user's role and ID (exposed as Angular signals: `isLoggedIn()`, `role()`, `currentUser()`), and
`authInterceptor` attaches the bearer token and does a one-shot silent refresh on a 401 using the
refresh token, before giving up and logging the user out.

## Known backend gaps this template works around

While wiring this up against the real controllers, a few endpoints implied by the SRS don't
exist yet in this backend build. Per SRS §8 ("raise missing requirements before
implementation"), they're flagged here and in code comments rather than silently papered over:

1. **No `/api/v1/addresses` controller.** The SRS data model defines `ShippingAddress`, and
   `CreateOrderRequest` needs a `shippingAddressId`, but there's no endpoint to create or list a
   customer's addresses. The checkout screen currently asks for a pasted address GUID
   (`features/orders/checkout`) — replace that field with a real picker once the endpoint exists.
2. **No "list my orders" endpoint.** `OrdersController` only has `POST /`, `GET /{id}`, and
   `POST /{id}/cancel` — nothing scoped to the calling customer. `OrderHistoryService` works
   around this by remembering order IDs placed from the current browser and resolving each via
   `GET /{id}`. Swap this for a real list call (e.g. `GET /api/v1/orders/mine`) once it exists.
3. **No "list all shipments" endpoint for Admin**, and `OrderDto` carries no `shipmentId` to
   bridge from an order to its shipment. `RecentLookupService` remembers shipment IDs seen in
   this browser (opened, or just assigned) so the admin shipment list has something to show.
   A real `GET /api/v1/shipments?status=&agentId=&page=` endpoint would remove the need for this.

None of these workarounds touch business logic — they're purely client-side conveniences so the
UI has something to list, clearly commented at each call site.

## Extending this template

- Swap the SCSS design tokens in `src/styles.scss` (`--color-*`, `--font-*`) for your brand.
- Real-time shipment updates: add a SignalR client (`@microsoft/signalr`) and have
  `ShipmentDetailComponent`/`TrackingLookupComponent` subscribe instead of only fetching once.
- Add pagination/filtering query params to `ProductService.getProducts` as the catalog grows.
- Once the address/list-orders/list-shipments endpoints above exist, delete the corresponding
  workaround service and wire the real call in one place.
