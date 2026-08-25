using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithItemsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
