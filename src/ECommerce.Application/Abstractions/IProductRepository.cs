using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Products, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        double? minRating,
        double? maxRating,
        bool? isAvailable,
        string? sortBy,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetRelatedProductsAsync(
        Guid productId,
        List<Guid> categoryIds,
        int count,
        CancellationToken cancellationToken = default);
}
