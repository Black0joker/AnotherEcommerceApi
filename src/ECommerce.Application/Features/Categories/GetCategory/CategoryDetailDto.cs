namespace ECommerce.Application.Features.Categories.GetCategory;

public record CategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int DisplayOrder,
    bool IsActive,
    int ProductCount,
    IReadOnlyList<ChildCategoryDto> Children
);

public record ChildCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder
);
