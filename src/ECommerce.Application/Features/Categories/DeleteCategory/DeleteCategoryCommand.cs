using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : ICommand;
