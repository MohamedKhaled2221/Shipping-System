using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly AppDbContext _dbContext;
    public ShipmentRepository(AppDbContext dbContext) => _dbContext = dbContext;

    private IQueryable<Shipment> ShipmentsWithChildren() =>
        _dbContext.Shipments
            .Include(s => s.StatusHistory)
            .Include(s => s.FailedDeliveryReasons);

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ShipmentsWithChildren().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        ShipmentsWithChildren().FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);

    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default) =>
        ShipmentsWithChildren().FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, cancellationToken);

    public async Task<IReadOnlyList<Shipment>> GetByDeliveryAgentIdAsync(Guid deliveryAgentId, CancellationToken cancellationToken = default) =>
        await ShipmentsWithChildren()
            .Where(s => s.DeliveryAgentId == deliveryAgentId)
            .ToListAsync(cancellationToken);

    public void Add(Shipment shipment) => _dbContext.Shipments.Add(shipment);
}
