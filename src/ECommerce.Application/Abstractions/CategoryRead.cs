namespace ECommerce.Application.Abstractions;

/// <summary>
/// Projected read model for the active category list: flat, cache-friendly
/// fields with parent name and product count computed server-side. No entity
/// graphs or circular navigations are serialized into the cache.
/// </summary>
public sealed record CategoryRead(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int DisplayOrder,
    int ProductCount);
