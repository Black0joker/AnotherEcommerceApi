namespace ECommerce.Application.Abstractions;

/// <summary>
/// Projected read model for related-product listings: exactly the fields the
/// endpoint displays, computed server-side. Flat and cache-friendly - no entity
/// graphs, no circular navigations, tiny serialized payloads.
/// </summary>
public sealed record RelatedProductRead(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    double AverageRating);
