using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetProduct;

public class GetProductQueryHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IReviewRepository _reviewRepository;

    public GetProductQueryHandler(
        IProductRepository productRepository,
        IReviewRepository reviewRepository)
    {
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.Id}' was not found."));
        }

        var averageRating = await _reviewRepository.GetAverageRatingAsync(product.Id, cancellationToken);

        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.SKU,
            product.Price,
            product.CompareAtPrice,
            product.IsActive,
            product.InventoryItem?.AvailableQuantity ?? 0,
            averageRating,
            product.Reviews.Count,
            product.CreatedAt
        );

        return Result.Success(dto);
    }
}
