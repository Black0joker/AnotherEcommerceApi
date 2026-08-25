using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
}
