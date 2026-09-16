using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;
    public CustomerRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email.Trim().ToLower(), cancellationToken);

    public void Add(Customer customer) => _dbContext.Customers.Add(customer);
}
