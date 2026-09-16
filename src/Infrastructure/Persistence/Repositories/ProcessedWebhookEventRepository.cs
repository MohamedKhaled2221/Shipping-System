using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class ProcessedWebhookEventRepository : IProcessedWebhookEventRepository
{
    private readonly AppDbContext _dbContext;
    public ProcessedWebhookEventRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> ExistsAsync(string providerName, string externalEventId, CancellationToken cancellationToken = default) =>
        _dbContext.ProcessedWebhookEvents
            .AnyAsync(e => e.ProviderName == providerName && e.ExternalEventId == externalEventId, cancellationToken);

    public void Add(ProcessedWebhookEvent processedEvent) => _dbContext.ProcessedWebhookEvents.Add(processedEvent);
}
