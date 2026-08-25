using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Products.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Check if SKU already exists
        var exists = await _productRepository.ExistsBySkuAsync(request.SKU, cancellationToken);
        if (exists)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "Product.SKUAlreadyExists",
                $"A product with SKU '{request.SKU}' already exists."));
        }

        // Create slug from name
        var slug = GenerateSlug(request.Name);

        var product = new Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            SKU = request.SKU,
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            IsActive = true
        };

        // Add to category if specified
        if (request.CategoryId.HasValue)
        {
            product.ProductCategories.Add(new ProductCategory
            {
                CategoryId = request.CategoryId.Value
            });
        }

        await _productRepository.AddAsync(product, cancellationToken);

        // Create inventory item
        var inventoryItem = new InventoryItem
        {
            ProductId = product.Id,
            AvailableQuantity = request.InitialStock
        };

        await _inventoryRepository.AddAsync(inventoryItem, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(product.Id);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        return slug.Trim('-');
    }
}
