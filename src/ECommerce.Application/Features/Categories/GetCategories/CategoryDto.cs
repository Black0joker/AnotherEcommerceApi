namespace ECommerce.Application.Features.Categories.GetCategories;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int DisplayOrder,
    int ProductCount
);
