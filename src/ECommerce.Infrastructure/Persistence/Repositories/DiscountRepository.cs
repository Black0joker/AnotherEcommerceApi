using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly ApplicationDbContext _context;

    public DiscountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Discount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Discounts
            .Include(d => d.DiscountProducts)
            .Include(d => d.DiscountCategories)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Discount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Discounts
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Discount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Discounts
            .Include(d => d.DiscountProducts)
            .Include(d => d.DiscountCategories)
            .FirstOrDefaultAsync(d => d.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Discounts
            .Where(d => d.IsActive
                && d.StartsAt <= now
                && (d.EndsAt == null || d.EndsAt.Value >= now)
                && (d.UsageLimit == null || d.UsedCount < d.UsageLimit.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Discounts.AnyAsync(d => d.Code == code, cancellationToken);
    }

    public async Task AddAsync(Discount entity, CancellationToken cancellationToken = default)
    {
        await _context.Discounts.AddAsync(entity, cancellationToken);
    }

    public void Update(Discount entity)
    {
        _context.Discounts.Update(entity);
    }

    public void Delete(Discount entity)
    {
        _context.Discounts.Remove(entity);
    }
}
