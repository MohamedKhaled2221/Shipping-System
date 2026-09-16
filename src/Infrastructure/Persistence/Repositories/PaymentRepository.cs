using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _dbContext;
    public PaymentRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> GetByTransactionRefAsync(string transactionRef, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.FirstOrDefaultAsync(p => p.TransactionRef == transactionRef, cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _dbContext.Payments
            .Where(p => p.OrderId == orderId)
            .ToListAsync(cancellationToken);

    public void Add(Payment payment) => _dbContext.Payments.Add(payment);
}
