using MediatR;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Auth.Commands.Login;

/// <summary>
/// FR-1.2 — "Admins and Delivery Agents have separate login flows." Implemented here as one
/// command parameterized by Role rather than three near-identical commands/handlers, with the
/// separation happening at the API layer instead: /api/v1/auth/customers/login,
/// /api/v1/auth/admins/login, and /api/v1/auth/agents/login each construct this command with
/// a fixed Role, so a customer credential can never accidentally authenticate against the
/// admin table (or vice versa) — the "separate flow" requirement is about which credential
/// store gets checked, and Role pins that at the call site, not about needing three
/// copy-pasted handlers.
///
/// Identifier is Email for Customer/AdminUser, Phone for DeliveryAgent (it has no Email
/// field — see Domain.Entities.DeliveryAgent).
/// </summary>
public sealed record LoginCommand(string Identifier, string Password, UserRole Role) : IRequest<AuthTokensDto>;
