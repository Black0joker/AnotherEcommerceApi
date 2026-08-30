using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Batch-loads role names for a set of users in one server-side query,
    /// keyed by user id. Users without roles are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRolesByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
    Task AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);
    Task RemoveFromRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        string? role,
        CancellationToken cancellationToken = default);
}
