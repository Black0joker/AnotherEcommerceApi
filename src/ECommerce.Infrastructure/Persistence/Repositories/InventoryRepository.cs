using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<InventoryItem>();
        }

        return await _context.InventoryItems
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryTransactions
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryItem entity, CancellationToken cancellationToken = default)
    {
        await _context.InventoryItems.AddAsync(entity, cancellationToken);
    }

    public void Update(InventoryItem entity)
    {
        _context.InventoryItems.Update(entity);
    }

    public void Delete(InventoryItem entity)
    {
        _context.InventoryItems.Remove(entity);
    }
}
