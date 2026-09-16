using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace ShippingSystem.Api.RateLimiting;

/// <summary>
/// NFR: Scalability/Availability — "system must scale horizontally... remain highly
/// available." Rate limiting is the first line of defense against a single abusive client
/// (or a runaway retry loop) degrading the API for everyone else, independent of and much
/// cheaper than any horizontal scaling. Uses .NET's built-in
/// System.Threading.RateLimiting/Microsoft.AspNetCore.RateLimiting — no external package —
/// so this lives entirely in Api, the only project that's ever supposed to know an HTTP
/// pipeline exists.
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // The default policy every endpoint gets unless it opts into something else
            // (via [EnableRateLimiting]/[DisableRateLimiting]). Partitioned by authenticated
            // user id when available, falling back to remote IP for anonymous callers —
            // this is why it's a GlobalLimiter (evaluated for every request) rather than a
            // named policy an attribute would have to opt every controller into individually.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = ResolvePartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // FR-1.1/1.2 brute-force protection. Deliberately keyed by IP even for an
            // authenticated-looking request — a credential-stuffing attempt against
            // /auth/customers/login has no valid JWT yet, so partitioning by user id (the
            // global limiter's usual key) would give every guessed password its own fresh
            // 100-request budget. Applied at the AuthController class level, so it covers
            // register/login/refresh uniformly rather than needing to be remembered per action.
            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        status = StatusCodes.Status429TooManyRequests,
                        title = "Too many requests. Please try again shortly."
                    },
                    cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress}";
    }
}
