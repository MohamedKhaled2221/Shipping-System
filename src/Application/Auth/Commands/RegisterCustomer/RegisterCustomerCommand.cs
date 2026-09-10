using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Auth.Commands.RegisterCustomer;

/// <summary>FR-1.1. Auto-issues tokens on success so the client doesn't need a separate login call right after registering.</summary>
public sealed record RegisterCustomerCommand(string Name, string Email, string Phone, string Password) : IRequest<AuthTokensDto>;
