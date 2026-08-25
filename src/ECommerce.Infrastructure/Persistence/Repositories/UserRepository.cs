using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.ToListAsync(cancellationToken);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return new List<string>();

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
    {
        await _userManager.AddToRoleAsync(user, role);
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
    {
        await _userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        string? role,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> query = _context.Users.AsQueryable();

        // When a role filter is supplied, resolve the member user IDs via the
        // UserManager first (Identity tables are not exposed as DbSets on this context).
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            var roleUserIds = roleUsers.Select(u => u.Id).ToHashSet();
            query = query.Where(u => roleUserIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public Task AddAsync(ApplicationUser entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(ApplicationUser entity)
    {
        _context.Users.Update(entity);
    }

    public void Delete(ApplicationUser entity)
    {
        _context.Users.Remove(entity);
    }
}
