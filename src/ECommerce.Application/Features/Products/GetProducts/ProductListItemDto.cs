namespace ECommerce.Application.Features.Products.GetProducts;

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsActive,
    int AvailableQuantity,
    DateTime CreatedAt
);
