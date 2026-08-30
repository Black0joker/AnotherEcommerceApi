using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetProduct;

public class GetProductQueryHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public GetProductQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        // One projected query: no entity graph, no Reviews include, and no
        // second round-trip for the average. Rating and review count come
        // from the persisted approved-review aggregates on Product.
        var product = await _productRepository.GetProductDetailAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.Id}' was not found."));
        }

        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.SKU,
            product.Price,
            product.CompareAtPrice,
            product.IsActive,
            product.AvailableQuantity,
            product.AverageRating,
            product.RatingCount,
            product.CreatedAt
        );

        return Result.Success(dto);
    }
}
