namespace ECommerce.Application.Features.Reviews.GetProductReviews;

public record ReviewDto(
    Guid Id,
    Guid ProductId,
    Guid UserId,
    string? UserName,
    int Rating,
    string? Title,
    string? Comment,
    bool IsApproved,
    DateTime CreatedAt
);
