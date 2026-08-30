using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Review> Reviews, int TotalCount)> GetApprovedByProductIdPagedAsync(
        Guid productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Review?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasUserPurchasedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<int> GetReviewCountAsync(Guid productId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Recomputes Product.AverageRating/RatingCount from approved reviews.
    /// Must be called whenever the approved review set for a product changes
    /// (approval, edit-induced re-approval reset, or deletion of an approved
    /// review) so listing filters/sorts stay consistent.
    /// </summary>
    Task RecalculateProductRatingAsync(Guid productId, CancellationToken cancellationToken = default);
}
