using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.GetCategory;

public record GetCategoryQuery(Guid Id) : IQuery<CategoryDetailDto>;
