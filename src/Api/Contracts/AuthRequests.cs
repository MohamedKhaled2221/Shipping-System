namespace ShippingSystem.Api.Contracts;

public sealed record RegisterCustomerRequest(string Name, string Email, string Phone, string Password);

/// <summary>Identifier is an email for /customers/login and /admins/login, a phone number for /agents/login.</summary>
public sealed record LoginRequest(string Identifier, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);
