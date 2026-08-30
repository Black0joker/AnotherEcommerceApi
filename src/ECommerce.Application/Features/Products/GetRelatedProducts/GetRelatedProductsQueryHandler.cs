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
        // Lightweight projected category lookup instead of loading the whole
        // detail graph just to read category ids. Null means "not found".
        var categoryIds = await _productRepository.GetCategoryIdsAsync(request.ProductId, cancellationToken);

        if (categoryIds is null)
        {
            return Result.Failure<IReadOnlyList<RelatedProductDto>>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.ProductId}' was not found."));
        }

        var relatedProducts = await _productRepository.GetRelatedProductsAsync(
            request.ProductId,
            categoryIds,
            request.Count,
            cancellationToken);

        // Ratings come from the persisted approved-review aggregates. The
        // navigation is not included on this path, and reading it used to
        // hit an empty p.Reviews collection so every rating reported 0.
        var dtos = relatedProducts.Select(p => new RelatedProductDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Price,
            p.CompareAtPrice,
            p.AverageRating
        )).ToList();

        return Result.Success<IReadOnlyList<RelatedProductDto>>(dtos);
    }
}
