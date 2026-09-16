using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class ShippingAddressRepository : IShippingAddressRepository
{
    private readonly AppDbContext _dbContext;
    public ShippingAddressRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<ShippingAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ShippingAddresses.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ShippingAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _dbContext.ShippingAddresses
            .Where(a => a.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public void Add(ShippingAddress address) => _dbContext.ShippingAddresses.Add(address);

    public void Remove(ShippingAddress address) => _dbContext.ShippingAddresses.Remove(address);
}
