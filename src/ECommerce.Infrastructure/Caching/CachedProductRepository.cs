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

    public async Task<ProductDetailRead?> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Pure read model - safe to cache. Update/Delete invalidate this key
        // via CacheKeys.ProductById; stock changes ride out the TTL.
        var cacheKey = CacheKeys.ProductById(id);

        var cached = await _cache.GetAsync<ProductDetailRead>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var detail = await _inner.GetProductDetailAsync(id, cancellationToken);

        if (detail is not null)
        {
            await _cache.SetAsync(cacheKey, detail, CacheKeys.ProductDetailExpiration, cancellationToken);
        }

        return detail;
    }

    public Task<List<Guid>?> GetCategoryIdsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Tiny projection feeding the related-products query; the related
        // list itself is cached, so this stays a plain pass-through.
        return _inner.GetCategoryIdsAsync(productId, cancellationToken);
    }

    public Task<bool> HasOrderItemsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Pure EXISTS probe - no cache needed.
        return _inner.HasOrderItemsAsync(productId, cancellationToken);
    }

    public async Task<ProductDetailRead?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        // Cache the flat projection, not an entity graph - small payload,
        // no circular navigations, no IgnoreCycles serialization cost.
        var cacheKey = CacheKeys.ProductBySlug(slug);

        var cached = await _cache.GetAsync<ProductDetailRead>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var detail = await _inner.GetBySlugAsync(slug, cancellationToken);

        if (detail is not null)
        {
            await _cache.SetAsync(cacheKey, detail, CacheKeys.ProductDetailExpiration, cancellationToken);
        }

        return detail;
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

    public async Task<IReadOnlyList<RelatedProductRead>> GetRelatedProductsAsync(
        Guid productId, List<Guid> categoryIds, int count, CancellationToken cancellationToken = default)
    {
        // Cache the projected read models - tiny flat payloads.
        var cacheKey = CacheKeys.ProductRelated(productId);

        var cached = await _cache.GetAsync<List<RelatedProductRead>>(cacheKey, cancellationToken);
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
        // IRepository<T> contract. Synchronous callers get the EF state change
        // only: cache invalidation happens exclusively in UpdateAsync so it is
        // awaited in the request flow instead of racing past the response as a
        // fire-and-forget task. All write-path handlers use UpdateAsync.
        _inner.Update(entity);
    }

    public void Delete(Product entity)
    {
        // IRepository<T> contract. See Update: invalidation is awaited in
        // DeleteAsync, never fired-and-forgotten here.
        _inner.Delete(entity);
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
