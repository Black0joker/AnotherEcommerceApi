using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reviews.GetProductReviews;

public record GetProductReviewsQuery(Guid ProductId) : IQuery<IReadOnlyList<ReviewDto>>;
