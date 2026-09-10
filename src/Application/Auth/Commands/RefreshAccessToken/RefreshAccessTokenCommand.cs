using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Auth.Commands.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : IRequest<AuthTokensDto>;
