using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class InventoryReservationRepository : IInventoryReservationRepository
{
    private readonly AppDbContext _dbContext;
    public InventoryReservationRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<InventoryReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryReservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InventoryReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _dbContext.InventoryReservations
            .Where(r => r.OrderId == orderId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryReservation>> GetExpiredReservedAsync(DateTime asOfUtc, CancellationToken cancellationToken = default) =>
        await _dbContext.InventoryReservations
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt <= asOfUtc)
            .ToListAsync(cancellationToken);

    public void Add(InventoryReservation reservation) => _dbContext.InventoryReservations.Add(reservation);
}
