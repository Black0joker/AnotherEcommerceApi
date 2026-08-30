using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reviews.GetProductReviews;

public class GetProductReviewsQueryHandler : IQueryHandler<GetProductReviewsQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetProductReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<PagedResult<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var (reviews, totalCount) = await _reviewRepository.GetApprovedByProductIdPagedAsync(
            request.ProductId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = reviews.Select(r => new ReviewDto(
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

        var result = new PagedResult<ReviewDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
