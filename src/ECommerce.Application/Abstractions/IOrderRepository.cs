using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
}
