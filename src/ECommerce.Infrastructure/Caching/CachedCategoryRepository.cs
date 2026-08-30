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

    public async Task<IReadOnlyList<CategoryRead>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // Cache the flat projection, not an entity graph - small payload,
        // no circular Parent/SubCategory navigations to serialize.
        var cacheKey = CacheKeys.CategoryList();

        var cached = await _cache.GetAsync<List<CategoryRead>>(cacheKey, cancellationToken);
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

    public Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        return UpdateCoreAsync(entity, cancellationToken);
    }

    public Task DeleteAsync(Category entity, CancellationToken cancellationToken = default)
    {
        return DeleteCoreAsync(entity, cancellationToken);
    }

    public void Update(Category entity)
    {
        // IRepository<T> contract. Synchronous callers get the EF state change
        // only: cache invalidation happens exclusively in UpdateAsync so it is
        // awaited in the request flow instead of racing past the response as a
        // fire-and-forget task. All write-path handlers use UpdateAsync.
        _inner.Update(entity);
    }

    public void Delete(Category entity)
    {
        // IRepository<T> contract. See Update: invalidation is awaited in
        // DeleteAsync, never fired-and-forgotten here.
        _inner.Delete(entity);
    }

    private async Task UpdateCoreAsync(Category entity, CancellationToken cancellationToken)
    {
        await _inner.UpdateAsync(entity, cancellationToken);
        await InvalidateCategoryCacheAsync(entity, cancellationToken);
    }

    private async Task DeleteCoreAsync(Category entity, CancellationToken cancellationToken)
    {
        await _inner.DeleteAsync(entity, cancellationToken);
        await InvalidateCategoryCacheAsync(entity, cancellationToken);
    }

    private async Task InvalidateCategoryCacheAsync(Category entity, CancellationToken cancellationToken)
    {
        // Best-effort cache invalidation. RedisCacheService never throws
        // (it degrades gracefully when Redis is down), so a failed invalidation
        // simply lets the entry expire via its TTL.
        await _cache.RemoveAsync(CacheKeys.CategoryById(entity.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.CategoryList(), cancellationToken);
    }
}
