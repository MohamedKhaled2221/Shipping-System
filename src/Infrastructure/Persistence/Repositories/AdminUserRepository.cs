using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _dbContext;
    public AdminUserRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.AdminUsers.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.AdminUsers.FirstOrDefaultAsync(a => a.Email == email.Trim().ToLower(), cancellationToken);

    public void Add(AdminUser admin) => _dbContext.AdminUsers.Add(admin);
}
