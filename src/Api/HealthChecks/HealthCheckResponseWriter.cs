using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShippingSystem.Api.HealthChecks;

/// <summary>
/// The default health check middleware just writes the overall status as plain text
/// ("Healthy"/"Unhealthy"), which is enough for a liveness probe but not enough to tell an
/// operator WHICH dependency failed on a readiness check. This writes each individual
/// check's name, status, description, and duration instead — the first thing anyone
/// debugging a failed deployment will look at.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static Task WriteAsync(HttpContext httpContext, HealthReport report)
    {
        httpContext.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message
            })
        };

        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
