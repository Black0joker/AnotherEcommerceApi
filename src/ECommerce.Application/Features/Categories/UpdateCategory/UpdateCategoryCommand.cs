using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder,
    bool IsActive
) : ICommand;
