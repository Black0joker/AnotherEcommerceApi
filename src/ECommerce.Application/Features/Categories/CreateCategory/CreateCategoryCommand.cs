using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder = 0
) : ICommand<Guid>;
