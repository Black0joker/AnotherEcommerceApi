using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IWishlistRepository : IRepository<Wishlist>
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Wishlist?> GetWithItemsAsync(Guid userId, CancellationToken cancellationToken = default);
}
