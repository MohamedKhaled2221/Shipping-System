using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ApplicationExceptions = ShippingSystem.Application.Common.Exceptions;
using DomainExceptions = ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Api.Middleware;

/// <summary>
/// The single place HTTP status codes get decided for every exception type the lower layers
/// throw. Domain and Application never reference HTTP at all (see their own docs) — this is
/// where that gap is deliberately closed, and closed in exactly one place.
///
/// Registered via `builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()` +
/// `app.UseExceptionHandler()` in Program.cs (the .NET 8+ IExceptionHandler pipeline, not the
/// older custom-middleware-with-try/catch pattern).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, extensions) = MapException(exception);

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        else
            _logger.LogWarning("{ExceptionType} on {Path}: {Message}", exception.GetType().Name, httpContext.Request.Path, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = httpContext.Request.Path
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
                problemDetails.Extensions[key] = value;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, Dictionary<string, object?>? Extensions) MapException(Exception exception) =>
        exception switch
        {
            // Request-shape/lookup failures — Application-level, never a broken business rule.
            ApplicationExceptions.NotFoundException ex =>
                (StatusCodes.Status404NotFound, ex.Message, null),

            ApplicationExceptions.ValidationException ex =>
                (StatusCodes.Status400BadRequest, "One or more validation errors occurred.",
                    new Dictionary<string, object?> { ["errors"] = ex.Errors }),

            ApplicationExceptions.UnauthorizedException ex =>
                (StatusCodes.Status401Unauthorized, ex.Message, null),

            // FR-5.3/6.3/6.5 — a stale RowVersion. 409 Conflict is the correct semantic here,
            // distinct from a validation failure: the request was well-formed, but the
            // resource moved under it.
            DomainExceptions.ConcurrencyConflictException ex =>
                (StatusCodes.Status409Conflict, ex.Message, null),

            // Every other business-rule violation (invalid state transition, insufficient
            // inventory, or a one-off DomainException raised inline) is a 400 — the request
            // was understood but violates a domain invariant.
            DomainExceptions.DomainException ex =>
                (StatusCodes.Status400BadRequest, ex.Message, null),

            // Deliberately generic: never leak exception.Message or a stack trace for anything
            // unclassified — that's how internal details end up in a client-facing response.
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };
}
