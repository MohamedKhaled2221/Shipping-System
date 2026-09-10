using MediatR;
using ShippingSystem.Application.Auth.Common;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Application.Auth.Commands.RegisterCustomer;

public sealed class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, AuthTokensDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCustomerCommandHandler(
        ICustomerRepository customers,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenStore refreshTokenStore,
        IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenStore = refreshTokenStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokensDto> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        // FR-1.1 + the unique index on Customers.Email (see Infrastructure) is the real
        // guarantee; this check just gives a friendlier error than a raw DB constraint failure.
        if (await _customers.GetByEmailAsync(request.Email, cancellationToken) is not null)
            throw new DomainException($"An account with email '{request.Email}' already exists.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var customer = Customer.Register(request.Name, request.Email, request.Phone, passwordHash);
        _customers.Add(customer);

        var tokens = await AuthTokenFactory.IssueAsync(
            _jwtTokenGenerator, _refreshTokenStore, customer.Id, UserRole.Customer, customer.Email, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }
}
