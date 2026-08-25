using ECommerce.Application.Common;
using ECommerce.Application.Features.Reviews.GetProductReviews;

namespace ECommerce.Application.Features.Reviews.UpdateReview;

public record UpdateReviewCommand(
    Guid ReviewId,
    int Rating,
    string? Title,
    string? Comment
) : ICommand<ReviewDto>;
