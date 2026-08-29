using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    /// <summary>
    /// Batch-loads products by id with only the category links included
    /// (no Reviews/OrderItems), for use cases like checkout that need
    /// authoritative price/availability data without heavy navigations.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    /// <summary>
    /// Loads a product with its read-side detail graph (category links,
    /// inventory and reviews) but without the order-item history. Use this
    /// for detail views and flows that need navigations; use the lightweight
    /// GetByIdAsync for existence checks and scalar-only flows.
    /// </summary>
    Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lightweight EXISTS check for order-item history, used to decide
    /// between soft delete (deactivation) and hard delete without loading
    /// every order item for the product.
    /// </summary>
    Task<bool> HasOrderItemsAsync(Guid productId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Async write operations. Implementations that need async side effects
    /// (e.g. cache invalidation in decorators) expose them here instead of
    /// blocking on async I/O inside the synchronous Update/Delete members.
    /// </summary>
    Task UpdateAsync(Product entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product entity, CancellationToken cancellationToken = default);
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
