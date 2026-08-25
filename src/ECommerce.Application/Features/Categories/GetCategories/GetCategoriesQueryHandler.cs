using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.GetCategories;

public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetActiveCategoriesAsync(cancellationToken);

        var dtos = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.ParentCategoryId,
            c.ParentCategory?.Name,
            c.DisplayOrder,
            c.ProductCategories.Count
        )).ToList();

        return Result.Success<IReadOnlyList<CategoryDto>>(dtos);
    }
}
