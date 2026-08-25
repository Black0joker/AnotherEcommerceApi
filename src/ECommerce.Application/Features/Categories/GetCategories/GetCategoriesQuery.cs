using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.GetCategories;

public record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
