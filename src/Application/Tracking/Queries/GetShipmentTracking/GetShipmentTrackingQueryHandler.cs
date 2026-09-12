using MediatR;
using ShippingSystem.Application.Common;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Tracking.Queries.GetShipmentTracking;

/// <summary>
/// NFR Caching. This endpoint is FR-9.2's public, unauthenticated, potentially
/// highest-traffic-per-shipment read in the whole system (a customer refreshing a tracking
/// page). Correctness is still guaranteed: ShipmentTrackingUpdateEventHandler (Shipments
/// module) evicts this exact key the moment ShipmentStatusChangedEvent fires, so the TTL
/// below is a safety net for a missed/failed eviction, not the primary freshness mechanism.
/// </summary>
public sealed class GetShipmentTrackingQueryHandler : IRequestHandler<GetShipmentTrackingQuery, ShipmentTrackingDto>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IShipmentRepository _shipments;
    private readonly ICacheService _cache;

    public GetShipmentTrackingQueryHandler(IShipmentRepository shipments, ICacheService cache)
    {
        _shipments = shipments;
        _cache = cache;
    }

    public async Task<ShipmentTrackingDto> Handle(GetShipmentTrackingQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ShipmentTracking(request.TrackingNumber);

        var cached = await _cache.GetAsync<ShipmentTrackingDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var shipment = await _shipments.GetByTrackingNumberAsync(request.TrackingNumber, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.TrackingNumber);

        var dto = new ShipmentTrackingDto(
            shipment.TrackingNumber,
            shipment.Status,
            shipment.DeliveryAgentId,
            shipment.EstimatedDeliveryDate,
            shipment.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new ShipmentStatusHistoryDto(h.Status, h.ChangedAt, h.ChangedBy, h.Notes))
                .ToList());

        await _cache.SetAsync(cacheKey, dto, CacheDuration, cancellationToken);

        return dto;
    }
}
