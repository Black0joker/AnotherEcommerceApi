namespace ECommerce.Application.Abstractions;

/// <summary>
/// Projected read model for the product detail page: everything the endpoint
/// needs in one server-side projection, without materializing an entity graph
/// (no Reviews include) or issuing a follow-up rating query.
/// </summary>
public sealed record ProductDetailRead(
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
    int RatingCount,
    DateTime CreatedAt);
