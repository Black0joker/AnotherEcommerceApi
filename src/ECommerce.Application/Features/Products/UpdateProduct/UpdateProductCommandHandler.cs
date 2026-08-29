using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.Id}' was not found."));
        }

        // Check if SKU is already used by another product
        if (product.SKU != request.SKU)
        {
            var skuExists = await _productRepository.ExistsBySkuAsync(request.SKU, cancellationToken);
            if (skuExists)
            {
                return Result.Failure(Error.Conflict(
                    "Product.SKUAlreadyExists",
                    $"A product with SKU '{request.SKU}' already exists."));
            }
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.SKU = request.SKU;
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.IsActive = request.IsActive;
        product.Slug = GenerateSlug(request.Name);

        // Update category associations
        product.ProductCategories.Clear();
        if (request.CategoryId.HasValue)
        {
            product.ProductCategories.Add(new Domain.Entities.ProductCategory
            {
                ProductId = product.Id,
                CategoryId = request.CategoryId.Value
            });
        }

        await _productRepository.UpdateAsync(product, cancellationToken);
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
