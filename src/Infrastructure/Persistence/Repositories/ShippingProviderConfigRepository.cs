using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class ShippingProviderConfigRepository : IShippingProviderConfigRepository
{
    private readonly AppDbContext _dbContext;
    public ShippingProviderConfigRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<ShippingProviderConfig?> GetByProviderNameAsync(string providerName, CancellationToken cancellationToken = default) =>
        _dbContext.ShippingProviderConfigs.FirstOrDefaultAsync(c => c.ProviderName == providerName, cancellationToken);

    public async Task<IReadOnlyList<ShippingProviderConfig>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.ShippingProviderConfigs
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);
}
