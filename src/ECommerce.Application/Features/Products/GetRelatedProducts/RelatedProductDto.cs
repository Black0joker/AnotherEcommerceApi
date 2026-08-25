namespace ECommerce.Application.Features.Products.GetRelatedProducts;

public record RelatedProductDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    double AverageRating
);
