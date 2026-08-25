using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.GetCategory;

public class GetCategoryQueryHandler : IQueryHandler<GetCategoryQuery, CategoryDetailDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<CategoryDetailDto>> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<CategoryDetailDto>(Error.NotFound(
                "Category.NotFound",
                $"Category with ID '{request.Id}' was not found."));
        }

        var children = await _categoryRepository.GetChildCategoriesAsync(category.Id, cancellationToken);

        var childDtos = children.Select(c => new ChildCategoryDto(
            c.Id,
            c.Name,
            c.Slug,
            c.DisplayOrder
        )).ToList();

        var dto = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ParentCategoryId,
            category.ParentCategory?.Name,
            category.DisplayOrder,
            category.IsActive,
            category.ProductCategories.Count,
            childDtos);

        return Result.Success(dto);
    }
}
