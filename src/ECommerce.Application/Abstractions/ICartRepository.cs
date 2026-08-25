using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Cart?> GetWithItemsAsync(Guid userId, CancellationToken cancellationToken = default);
}
