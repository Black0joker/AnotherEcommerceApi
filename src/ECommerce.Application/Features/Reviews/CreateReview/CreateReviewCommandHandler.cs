using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Reviews.GetProductReviews;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Reviews.CreateReview;

public class CreateReviewCommandHandler : ICommandHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReviewCommandHandler(
        IReviewRepository reviewRepository,
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReviewDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<ReviewDto>(Error.Unauthorized(
                "Review.Unauthorized",
                "You must be authenticated to create a review."));
        }

        // Validate rating
        if (request.Rating < 1 || request.Rating > 5)
        {
            return Result.Failure<ReviewDto>(Error.Validation(
                "Review.InvalidRating",
                "Rating must be between 1 and 5."));
        }

        // Validate product exists
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ReviewDto>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.ProductId}' was not found."));
        }

        // Validate user has purchased the product
        var hasPurchased = await _reviewRepository.HasUserPurchasedProductAsync(userId.Value, request.ProductId, cancellationToken);
        if (!hasPurchased)
        {
            return Result.Failure<ReviewDto>(Error.Validation(
                "Review.PurchaseRequired",
                "You can only review a product after purchasing it."));
        }

        // Prevent duplicate reviews
        var hasReviewed = await _reviewRepository.HasUserReviewedProductAsync(userId.Value, request.ProductId, cancellationToken);
        if (hasReviewed)
        {
            return Result.Failure<ReviewDto>(Error.Validation(
                "Review.AlreadyReviewed",
                "You have already reviewed this product."));
        }

        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = userId.Value,
            Rating = request.Rating,
            Title = request.Title,
            Comment = request.Comment,
            IsApproved = false // Requires admin approval
        };

        await _reviewRepository.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ReviewDto(
            review.Id,
            review.ProductId,
            review.UserId,
            null,
            review.Rating,
            review.Title,
            review.Comment,
            review.IsApproved,
            review.CreatedAt);

        return Result.Success(dto);
    }
}
