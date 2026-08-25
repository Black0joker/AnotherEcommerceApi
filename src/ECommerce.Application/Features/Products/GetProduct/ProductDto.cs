namespace ECommerce.Application.Features.Products.GetProduct;

public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsActive,
    int AvailableQuantity,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt
);
