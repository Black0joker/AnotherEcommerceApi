using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IReviewRepository : IRepository<Review>
{
    Task<(IReadOnlyList<Review> Reviews, int TotalCount)> GetByProductAsync(
        Guid productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid reviewId, CancellationToken cancellationToken = default);
}
