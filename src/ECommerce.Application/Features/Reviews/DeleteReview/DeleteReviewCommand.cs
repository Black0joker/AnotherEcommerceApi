using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reviews.DeleteReview;

public record DeleteReviewCommand(Guid ReviewId) : ICommand<bool>;
