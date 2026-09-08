using MediatR;
using Microsoft.Extensions.Logging;

namespace ShippingSystem.Application.Common.Behaviors;

/// <summary>
/// NFR "Observability" — structured logging around every request. Logs request name +
/// elapsed time; on exception, logs the failure with the request payload before rethrowing
/// so the API's global exception middleware still owns the HTTP-mapping decision.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = DateTime.UtcNow;

        try
        {
            var response = await next();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName, (DateTime.UtcNow - startedAt).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {RequestName}: {Request}", requestName, request);
            throw;
        }
    }
}
