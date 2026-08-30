using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Batch-loads inventory items for a set of products in a single query.
    /// </summary>
    Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<InventoryTransaction> Transactions, int TotalCount)> GetTransactionsByProductIdPagedAsync(
        Guid productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
}
