using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Caching;

/// <summary>
/// Decorator around ProductRepository that adds Redis caching (cache-aside pattern).
/// Caches product details and related products.
/// Invalidates cache on updates.
/// </summary>
public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly ICacheService _cache;

    public CachedProductRepository(IProductRepository inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Intentionally NOT cached. This method also feeds the write path
        // (Update/Delete handlers), which must mutate fresh, change-tracked
        // entities. Returning a deserialized, detached entity there would make
        // EF attach a stale object graph and overwrite related rows on save.
        // Read-side caching is done on read-only members (slug, related) and via
        // DTO caching for the detail endpoint.
        return _inner.GetByIdAsync(id, cancellationToken);
    }

    public Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Intentionally NOT cached for the same reason as GetByIdAsync: this
        // method also feeds the write path (UpdateProduct), which must mutate
        // fresh, change-tracked entities.
        return _inner.GetByIdWithDetailsAsync(id, cancellationToken);
    }

    public Task<bool> HasOrderItemsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Pure EXISTS probe - no cache needed.
        return _inner.HasOrderItemsAsync(productId, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ProductBySlug(slug);

        var cached = await _cache.GetAsync<Product>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var product = await _inner.GetBySlugAsync(slug, cancellationToken);

        if (product is not null)
        {
            await _cache.SetAsync(cacheKey, product, CacheKeys.ProductDetailExpiration, cancellationToken);
        }

        return product;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Don't cache GetAll - too much data
        return _inner.GetAllAsync(cancellationToken);
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        // Don't cache by SKU - used for admin operations
        return _inner.GetBySkuAsync(sku, cancellationToken);
    }

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        // Batch loads are used by checkout to validate fresh prices/stock;
        // always hit the database.
        return _inner.GetByIdsAsync(ids, cancellationToken);
    }

    public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        return UpdateCoreAsync(entity, cancellationToken);
    }

    public Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
    {
        return DeleteCoreAsync(entity, cancellationToken);
    }

    public Task<(IReadOnlyList<Product> Products, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? search, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice, double? minRating, double? maxRating,
        bool? isAvailable, string? sortBy, CancellationToken cancellationToken = default)
    {
        // Product listings are too dynamic to cache effectively
        // (many filter combinations, frequently changing)
        return _inner.GetPagedAsync(pageNumber, pageSize, search, categoryId,
            minPrice, maxPrice, minRating, maxRating, isAvailable, sortBy, cancellationToken);
    }

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return _inner.ExistsBySkuAsync(sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(
        Guid productId, List<Guid> categoryIds, int count, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ProductRelated(productId);

        var cached = await _cache.GetAsync<List<Product>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var products = await _inner.GetRelatedProductsAsync(productId, categoryIds, count, cancellationToken);

        if (products.Count > 0)
        {
            await _cache.SetAsync(cacheKey, products.ToList(), CacheKeys.ProductListExpiration, cancellationToken);
        }

        return products;
    }

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await _inner.AddAsync(entity, cancellationToken);
    }

    public void Update(Product entity)
    {
        // IRepository<T> contract. Prefer UpdateAsync - this member schedules
        // cache invalidation without blocking (the cache service swallows its
        // own failures, so fire-and-forget here is safe).
        _ = UpdateCoreAsync(entity, CancellationToken.None);
    }

    public void Delete(Product entity)
    {
        // IRepository<T> contract. Prefer DeleteAsync - see Update for why
        // invalidation is fire-and-forget instead of blocking.
        _ = DeleteCoreAsync(entity, CancellationToken.None);
    }

    private async Task UpdateCoreAsync(Product entity, CancellationToken cancellationToken)
    {
        await _inner.UpdateAsync(entity, cancellationToken);
        await InvalidateProductCacheAsync(entity, cancellationToken);
    }

    private async Task DeleteCoreAsync(Product entity, CancellationToken cancellationToken)
    {
        await _inner.DeleteAsync(entity, cancellationToken);
        await InvalidateProductCacheAsync(entity, cancellationToken);
    }

    private async Task InvalidateProductCacheAsync(Product entity, CancellationToken cancellationToken)
    {
        // Best-effort cache invalidation. RedisCacheService never throws
        // (it degrades gracefully when Redis is down), so a failed invalidation
        // simply lets the entry expire via its TTL.
        await _cache.RemoveAsync(CacheKeys.ProductById(entity.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.ProductBySlug(entity.Slug), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.ProductRelated(entity.Id), cancellationToken);
    }
}
