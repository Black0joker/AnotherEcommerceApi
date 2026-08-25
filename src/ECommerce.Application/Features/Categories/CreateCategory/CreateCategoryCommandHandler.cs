using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Categories.CreateCategory;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = GenerateSlug(request.Name);

        // Check if slug already exists
        var exists = await _categoryRepository.ExistsBySlugAsync(slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "Category.SlugAlreadyExists",
                $"A category with slug '{slug}' already exists."));
        }

        // Validate parent category if specified
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Guid>(Error.NotFound(
                    "Category.ParentNotFound",
                    $"Parent category with ID '{request.ParentCategoryId}' was not found."));
            }
        }

        var category = new Category
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(category.Id);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        return slug.Trim('-');
    }
}
