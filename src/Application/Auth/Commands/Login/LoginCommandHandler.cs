using MediatR;
using ShippingSystem.Application.Auth.Common;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokensDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IAdminUserRepository _admins;
    private readonly IDeliveryAgentRepository _deliveryAgents;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        ICustomerRepository customers,
        IAdminUserRepository admins,
        IDeliveryAgentRepository deliveryAgents,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenStore refreshTokenStore,
        IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _admins = admins;
        _deliveryAgents = deliveryAgents;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenStore = refreshTokenStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var (userId, email, passwordHash) = request.Role switch
        {
            UserRole.Customer => await LookupCustomerAsync(request.Identifier, cancellationToken),
            UserRole.Admin => await LookupAdminAsync(request.Identifier, cancellationToken),
            UserRole.DeliveryAgent => await LookupDeliveryAgentAsync(request.Identifier, cancellationToken),
            _ => throw new UnauthorizedException("Invalid credentials.")
        };

        // Same exception/message whether the identifier doesn't exist or the password is
        // wrong — never let a client distinguish "no such account" from "wrong password."
        if (userId is null || !_passwordHasher.Verify(request.Password, passwordHash!))
            throw new UnauthorizedException("Invalid credentials.");

        var tokens = await AuthTokenFactory.IssueAsync(
            _jwtTokenGenerator, _refreshTokenStore, userId.Value, request.Role, email!, cancellationToken);

        // Only the RefreshTokenRecord (staged by IssueAsync above) needs committing here —
        // login doesn't mutate the Customer/AdminUser/DeliveryAgent aggregate itself.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    private async Task<(Guid? UserId, string? Email, string? PasswordHash)> LookupCustomerAsync(string email, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByEmailAsync(email, cancellationToken);
        return (customer?.Id, customer?.Email, customer?.PasswordHash);
    }

    private async Task<(Guid? UserId, string? Email, string? PasswordHash)> LookupAdminAsync(string email, CancellationToken cancellationToken)
    {
        var admin = await _admins.GetByEmailAsync(email, cancellationToken);
        return (admin?.Id, admin?.Email, admin?.PasswordHash);
    }

    private async Task<(Guid? UserId, string? Email, string? PasswordHash)> LookupDeliveryAgentAsync(string phone, CancellationToken cancellationToken)
    {
        var agent = await _deliveryAgents.GetByPhoneAsync(phone, cancellationToken);
        // DeliveryAgent has no Email — the JWT's email claim is left as the phone number for
        // this role; consumers should key off the Role claim, not assume email is always an email.
        return (agent?.Id, agent?.Phone, agent?.PasswordHash);
    }
}
