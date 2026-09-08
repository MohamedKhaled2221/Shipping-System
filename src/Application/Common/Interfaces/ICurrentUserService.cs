using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Common.Interfaces;

/// <summary>Populated from the JWT claims by an Infrastructure/API-layer implementation.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}

/// <summary>
/// Thin wrapper over DateTime.UtcNow so command handlers and validators (e.g. "expiry window
/// elapsed?", FR-5.6) are unit-testable without depending on wall-clock time.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
