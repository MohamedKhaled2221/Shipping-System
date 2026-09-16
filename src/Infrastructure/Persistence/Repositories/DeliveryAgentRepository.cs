using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class DeliveryAgentRepository : IDeliveryAgentRepository
{
    private readonly AppDbContext _dbContext;
    public DeliveryAgentRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<DeliveryAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.DeliveryAgents.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<DeliveryAgent?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        _dbContext.DeliveryAgents.FirstOrDefaultAsync(a => a.Phone == phone, cancellationToken);

    public async Task<IReadOnlyList<DeliveryAgent>> GetAvailableAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.DeliveryAgents
            .Where(a => a.IsAvailable)
            .ToListAsync(cancellationToken);

    public void Add(DeliveryAgent agent) => _dbContext.DeliveryAgents.Add(agent);
}
