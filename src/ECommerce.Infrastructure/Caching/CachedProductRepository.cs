using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

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
        _inner.Update(entity);

        // Invalidate cache for this product
        var cacheKey = CacheKeys.ProductById(entity.Id);
        _cache.RemoveAsync(cacheKey).GetAwaiter().GetResult();

        // Invalidate by slug too
        var slugKey = CacheKeys.ProductBySlug(entity.Slug);
        _cache.RemoveAsync(slugKey).GetAwaiter().GetResult();

        // Invalidate related products
        var relatedKey = CacheKeys.ProductRelated(entity.Id);
        _cache.RemoveAsync(relatedKey).GetAwaiter().GetResult();
    }

    public void Delete(Product entity)
    {
        _inner.Delete(entity);

        // Invalidate cache for this product
        var cacheKey = CacheKeys.ProductById(entity.Id);
        _cache.RemoveAsync(cacheKey).GetAwaiter().GetResult();

        var slugKey = CacheKeys.ProductBySlug(entity.Slug);
        _cache.RemoveAsync(slugKey).GetAwaiter().GetResult();

        var relatedKey = CacheKeys.ProductRelated(entity.Id);
        _cache.RemoveAsync(relatedKey).GetAwaiter().GetResult();
    }
}
