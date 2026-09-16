using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _dbContext;
    public NotificationRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public void Add(Notification notification) => _dbContext.Notifications.Add(notification);
}
