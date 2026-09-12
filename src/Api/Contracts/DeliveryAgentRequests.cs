namespace ShippingSystem.Api.Contracts;

/// <summary>Admin onboarding a new agent (FR-7.1). No self-registration route exists for this role — see RegisterDeliveryAgentCommand's doc comment.</summary>
public sealed record RegisterDeliveryAgentRequest(string Name, string Phone, string Password);

public sealed record UpdateAgentProfileRequest(string Name, string Phone);

public sealed record SetAgentAvailabilityRequest(bool IsAvailable);
