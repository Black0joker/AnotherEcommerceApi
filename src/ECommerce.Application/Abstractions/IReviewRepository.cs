using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Review?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasUserPurchasedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<int> GetReviewCountAsync(Guid productId, CancellationToken cancellationToken = default);
}
