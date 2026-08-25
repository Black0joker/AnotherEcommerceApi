using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Categories.UpdateCategory;

public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(Error.NotFound(
                "Category.NotFound",
                $"Category with ID '{request.Id}' was not found."));
        }

        // Validate parent category if specified
        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == request.Id)
            {
                return Result.Failure(Error.Validation(
                    "Category.CannotBeOwnParent",
                    "A category cannot be its own parent."));
            }

            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken);
            if (parent is null)
            {
                return Result.Failure(Error.NotFound(
                    "Category.ParentNotFound",
                    $"Parent category with ID '{request.ParentCategoryId}' was not found."));
            }
        }

        category.Name = request.Name;
        category.Slug = GenerateSlug(request.Name);
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        return slug.Trim('-');
    }
}
