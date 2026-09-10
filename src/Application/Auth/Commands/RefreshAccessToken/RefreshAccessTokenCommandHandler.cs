using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Auth.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, AuthTokensDto>
{
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshAccessTokenCommandHandler(
        IRefreshTokenStore refreshTokenStore, IJwtTokenGenerator jwtTokenGenerator, IUnitOfWork unitOfWork)
    {
        _refreshTokenStore = refreshTokenStore;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokensDto> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        // ValidateAndRotateAsync both checks validity AND immediately revokes the token just
        // used, staging the replacement — see IRefreshTokenStore's own docs on why rotation matters.
        var session = await _refreshTokenStore.ValidateAndRotateAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Refresh token is invalid, expired, or already used.");

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(session.UserId, session.Role, session.Email);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            session.UserId,
            session.Role.ToString(),
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            session.NewRefreshToken.Token,
            session.NewRefreshToken.ExpiresAtUtc);
    }
}
