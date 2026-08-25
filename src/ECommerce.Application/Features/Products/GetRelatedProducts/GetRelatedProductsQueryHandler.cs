using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetRelatedProducts;

public class GetRelatedProductsQueryHandler : IQueryHandler<GetRelatedProductsQuery, IReadOnlyList<RelatedProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetRelatedProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyList<RelatedProductDto>>> Handle(
        GetRelatedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            return Result.Failure<IReadOnlyList<RelatedProductDto>>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.ProductId}' was not found."));
        }

        // Get products from the same category
        var categoryIds = product.ProductCategories.Select(pc => pc.CategoryId).ToList();

        var relatedProducts = await _productRepository.GetRelatedProductsAsync(
            request.ProductId,
            categoryIds,
            request.Count,
            cancellationToken);

        var dtos = relatedProducts.Select(p => new RelatedProductDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Price,
            p.CompareAtPrice,
            p.Reviews.Count > 0 ? p.Reviews.Average(r => r.Rating) : 0
        )).ToList();

        return Result.Success<IReadOnlyList<RelatedProductDto>>(dtos);
    }
}
