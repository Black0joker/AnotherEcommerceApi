using ECommerce.Application.Common;
using ECommerce.Application.Features.Reviews.GetProductReviews;

namespace ECommerce.Application.Features.Reviews.CreateReview;

public record CreateReviewCommand(
    Guid ProductId,
    int Rating,
    string? Title,
    string? Comment
) : ICommand<ReviewDto>;
