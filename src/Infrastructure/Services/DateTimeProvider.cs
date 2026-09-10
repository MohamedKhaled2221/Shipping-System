using ShippingSystem.Application.Common.Interfaces;

namespace ShippingSystem.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
