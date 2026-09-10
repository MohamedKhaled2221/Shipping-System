using Microsoft.AspNetCore.Http;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Infrastructure.Services;

/// <summary>
/// Reads the identity populated by the API's JWT authentication middleware (Auth module,
/// not built yet). Only depends on Microsoft.AspNetCore.Http.Abstractions (a lightweight
/// contracts package, not the full ASP.NET Core framework), so this stays a valid
/// Infrastructure-layer class rather than pulling in Kestrel/MVC.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var value = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }
}
