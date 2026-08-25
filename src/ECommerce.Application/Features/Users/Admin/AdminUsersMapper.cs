using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Users.Admin;

public static class AdminUsersMapper
{
    public static AdminUserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles)
    {
        return new AdminUserDto(
            user.Id,
            user.Email,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            roles);
    }
}
