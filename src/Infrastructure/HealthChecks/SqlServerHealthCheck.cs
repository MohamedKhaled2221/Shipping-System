using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ShippingSystem.Infrastructure.Persistence;

namespace ShippingSystem.Infrastructure.HealthChecks;

/// <summary>
/// Readiness check for FR-2.3/§9 style database access — "is SQL Server actually reachable
/// right now," not "did EF Core map correctly" (that's what a real query exercises, not a
/// health probe). Uses IDbContextFactory rather than the ambient scoped AppDbContext for the
/// same reason TrackingNumberGenerator/NotificationDispatcher do: the health-check pipeline
/// can run at arbitrary times relative to any request's own unit of work, so it must never
/// share a DbContext instance that some other in-flight operation might be mutating.
/// </summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public SqlServerHealthCheck(IDbContextFactory<AppDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // CanConnectAsync opens and immediately closes a connection — enough to prove
            // the server is reachable and credentials/connection string are valid, without
            // the cost or side effects of a real query against any table.
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server did not accept a connection.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server health check threw an exception.", ex);
        }
    }
}
