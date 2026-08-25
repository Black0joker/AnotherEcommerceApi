using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
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
