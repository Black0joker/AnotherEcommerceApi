using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reviews.GetProductReviews;

public class GetProductReviewsQueryHandler : IQueryHandler<GetProductReviewsQuery, IReadOnlyList<ReviewDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetProductReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<IReadOnlyList<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        var dtos = reviews
            .Where(r => r.IsApproved)
            .Select(r => new ReviewDto(
                r.Id,
                r.ProductId,
                r.UserId,
                r.User?.UserName,
                r.Rating,
                r.Title,
                r.Comment,
                r.IsApproved,
                r.CreatedAt
            )).ToList();

        return Result.Success<IReadOnlyList<ReviewDto>>(dtos);
    }
}
