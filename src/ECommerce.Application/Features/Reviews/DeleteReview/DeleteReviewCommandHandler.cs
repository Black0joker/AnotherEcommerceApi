using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reviews.DeleteReview;

public class DeleteReviewCommandHandler : ICommandHandler<DeleteReviewCommand, bool>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReviewCommandHandler(
        IReviewRepository reviewRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<bool>(Error.Unauthorized(
                "Review.Unauthorized",
                "You must be authenticated to delete a review."));
        }

        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Review.NotFound",
                $"Review with ID '{request.ReviewId}' was not found."));
        }

        // Only the review owner or admin can delete it
        var isAdmin = _currentUserService.IsInRole("Admin");
        if (review.UserId != userId.Value && !isAdmin)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Review.NotFound",
                $"Review with ID '{request.ReviewId}' was not found."));
        }

        var wasApproved = review.IsApproved;

        _reviewRepository.Delete(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Removing an approved review changes the product's persisted rating
        // aggregates; recompute them after the delete is persisted.
        if (wasApproved)
        {
            await _reviewRepository.RecalculateProductRatingAsync(review.ProductId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(true);
    }
}
