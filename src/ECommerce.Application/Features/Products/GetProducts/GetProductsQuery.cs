using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetProducts;

public class GetProductsQuery : PagedQuery, IQuery<PagedResult<ProductListItemDto>>
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public double? MinRating { get; init; }
    public double? MaxRating { get; init; }
    public bool? IsAvailable { get; init; }
    public string? SortBy { get; init; }
}
