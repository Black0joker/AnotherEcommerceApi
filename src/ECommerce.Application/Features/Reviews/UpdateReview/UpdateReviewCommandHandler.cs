using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Reviews.GetProductReviews;

namespace ECommerce.Application.Features.Reviews.UpdateReview;

public class UpdateReviewCommandHandler : ICommandHandler<UpdateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateReviewCommandHandler(
        IReviewRepository reviewRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReviewDto>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<ReviewDto>(Error.Unauthorized(
                "Review.Unauthorized",
                "You must be authenticated to update a review."));
        }

        // Validate rating
        if (request.Rating < 1 || request.Rating > 5)
        {
            return Result.Failure<ReviewDto>(Error.Validation(
                "Review.InvalidRating",
                "Rating must be between 1 and 5."));
        }

        var review = await _reviewRepository.GetByIdWithDetailsAsync(request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure<ReviewDto>(Error.NotFound(
                "Review.NotFound",
                $"Review with ID '{request.ReviewId}' was not found."));
        }

        // Only the review owner can update it
        if (review.UserId != userId.Value)
        {
            return Result.Failure<ReviewDto>(Error.NotFound(
                "Review.NotFound",
                $"Review with ID '{request.ReviewId}' was not found."));
        }

        review.Rating = request.Rating;
        review.Title = request.Title;
        review.Comment = request.Comment;
        review.IsApproved = false; // Requires re-approval after edit

        _reviewRepository.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ReviewDto(
            review.Id,
            review.ProductId,
            review.UserId,
            review.User?.UserName,
            review.Rating,
            review.Title,
            review.Comment,
            review.IsApproved,
            review.CreatedAt);

        return Result.Success(dto);
    }
}
