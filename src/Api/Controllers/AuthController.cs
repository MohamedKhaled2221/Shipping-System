using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShippingSystem.Api.Contracts;
using ShippingSystem.Api.RateLimiting;
using ShippingSystem.Application.Auth.Commands.Login;
using ShippingSystem.Application.Auth.Commands.RefreshAccessToken;
using ShippingSystem.Application.Auth.Commands.RegisterCustomer;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Api.Controllers;

/// <summary>
/// FR-1.1, FR-1.2. Three distinct login routes — not one generic "/login" with a role field
/// in the body — is the actual mechanism behind "separate login flows": the Role passed into
/// LoginCommand is fixed by which route was called, not by anything the client can set, so a
/// leaked customer password can never be used to probe the admin or agent login flow.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    public AuthController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpPost("customers/register")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensDto>> RegisterCustomer(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCustomerCommand(request.Name, request.Email, request.Phone, request.Password),
            cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("customers/login")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public Task<ActionResult<AuthTokensDto>> LoginCustomer(LoginRequest request, CancellationToken cancellationToken) =>
        LoginAsAsync(request, UserRole.Customer, cancellationToken);

    [AllowAnonymous]
    [HttpPost("admins/login")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public Task<ActionResult<AuthTokensDto>> LoginAdmin(LoginRequest request, CancellationToken cancellationToken) =>
        LoginAsAsync(request, UserRole.Admin, cancellationToken);

    [AllowAnonymous]
    [HttpPost("agents/login")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public Task<ActionResult<AuthTokensDto>> LoginAgent(LoginRequest request, CancellationToken cancellationToken) =>
        LoginAsAsync(request, UserRole.DeliveryAgent, cancellationToken);

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensDto>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(result);
    }

    private async Task<ActionResult<AuthTokensDto>> LoginAsAsync(LoginRequest request, UserRole role, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LoginCommand(request.Identifier, request.Password, role), cancellationToken);
        return Ok(result);
    }
}
