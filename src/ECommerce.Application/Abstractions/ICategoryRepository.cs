using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Async write operations. Implementations that need async side effects
    /// (e.g. cache invalidation in decorators) expose them here instead of
    /// blocking on async I/O inside the synchronous Update/Delete members.
    /// </summary>
    Task UpdateAsync(Category entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Category entity, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetChildCategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
