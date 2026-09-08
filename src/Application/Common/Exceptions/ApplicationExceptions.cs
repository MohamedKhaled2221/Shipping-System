using FluentValidation.Results;

namespace ShippingSystem.Application.Common.Exceptions;

/// <summary>Thrown when a requested aggregate does not exist. API middleware maps this to HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}

/// <summary>
/// Thrown by ValidationBehavior when a FluentValidation validator fails for an incoming
/// command/query. Deliberately distinct from Domain.Exceptions.DomainException — this
/// represents malformed INPUT (FR: input validation via FluentValidation), not a broken
/// business invariant. API middleware maps this to HTTP 400 with a field-error payload.
/// </summary>
public sealed class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

/// <summary>
/// Thrown for a failed login (unknown identifier OR wrong password — deliberately the same
/// exception/message for both, so a client can't enumerate which accounts exist by probing
/// the error text) and for a rejected/expired refresh token. API middleware maps this to
/// HTTP 401, distinct from ForbiddenException's 403 (authenticated but not allowed) —
/// nothing in this module needs 403 yet since role checks are enforced declaratively via
/// [Authorize(Roles = ...)] rather than thrown from a handler.
/// </summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
