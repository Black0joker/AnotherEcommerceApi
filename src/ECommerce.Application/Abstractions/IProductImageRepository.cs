using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IProductImageRepository : IRepository<ProductImage>
{
    Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetPrimaryByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
