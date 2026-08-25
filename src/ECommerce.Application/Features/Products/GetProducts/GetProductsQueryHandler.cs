using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetProducts;

public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<ProductListItemDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _productRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.CategoryId,
            request.SortBy,
            request.SortDirection,
            cancellationToken);

        var dtos = products.Select(p => new ProductListItemDto(
            p.Id,
            p.Name,
            p.Slug,
            p.SKU,
            p.Price,
            p.CompareAtPrice,
            p.IsActive,
            p.InventoryItem?.AvailableQuantity ?? 0,
            p.CreatedAt
        )).ToList();

        var result = new PagedResult<ProductListItemDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
