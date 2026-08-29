using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Caching;

/// <summary>
/// Decorator around CategoryRepository that adds Redis caching (cache-aside pattern).
/// Caches category list and category details.
/// Invalidates cache on updates.
/// </summary>
public class CachedCategoryRepository : ICategoryRepository
{
    private readonly ICategoryRepository _inner;
    private readonly ICacheService _cache;

    public CachedCategoryRepository(ICategoryRepository inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Intentionally NOT cached. This method also feeds the write path
        // (Update/Delete handlers), which must mutate fresh, change-tracked
        // entities. Returning a deserialized, detached entity there would make
        // EF attach a stale object graph and overwrite related rows on save.
        // Read-side caching is done on the read-only active list.
        return _inner.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _inner.GetAllAsync(cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        // Look up by slug - don't cache this, just delegate
        return await _inner.GetBySlugAsync(slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.CategoryList();

        var cached = await _cache.GetAsync<List<Category>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var categories = await _inner.GetActiveCategoriesAsync(cancellationToken);

        if (categories.Count > 0)
        {
            await _cache.SetAsync(cacheKey, categories.ToList(), CacheKeys.CategoryExpiration, cancellationToken);
        }

        return categories;
    }

    public Task<IReadOnlyList<Category>> GetChildCategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return _inner.GetChildCategoriesAsync(parentCategoryId, cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _inner.ExistsBySlugAsync(slug, cancellationToken);
    }

    public async Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await _inner.AddAsync(entity, cancellationToken);
        // Invalidate category list cache
        await _cache.RemoveAsync(CacheKeys.CategoryList(), cancellationToken);
    }

    public void Update(Category entity)
    {
        _inner.Update(entity);

        // Invalidate caches
        var cacheKey = CacheKeys.CategoryById(entity.Id);
        _cache.RemoveAsync(cacheKey).GetAwaiter().GetResult();
        _cache.RemoveAsync(CacheKeys.CategoryList()).GetAwaiter().GetResult();
    }

    public void Delete(Category entity)
    {
        _inner.Delete(entity);

        // Invalidate caches
        var cacheKey = CacheKeys.CategoryById(entity.Id);
        _cache.RemoveAsync(cacheKey).GetAwaiter().GetResult();
        _cache.RemoveAsync(CacheKeys.CategoryList()).GetAwaiter().GetResult();
    }
}
