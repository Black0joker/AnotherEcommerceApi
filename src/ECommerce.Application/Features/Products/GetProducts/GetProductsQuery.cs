using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetProducts;

public class GetProductsQuery : PagedQuery, IQuery<PagedResult<ProductListItemDto>>
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
