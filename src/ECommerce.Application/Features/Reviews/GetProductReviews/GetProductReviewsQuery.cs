using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reviews.GetProductReviews;

public class GetProductReviewsQuery : PagedQuery, IQuery<PagedResult<ReviewDto>>
{
    public Guid ProductId { get; set; }
}
