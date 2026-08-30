using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithItemsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    /// <summary>
    /// Idempotency lookup scoped to the user so it seeks on the leading key
    /// columns of the filtered unique index IX_Orders_UserId_IdempotencyKey.
    /// </summary>
    Task<Order?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetAllPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetByUserIdPagedAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
