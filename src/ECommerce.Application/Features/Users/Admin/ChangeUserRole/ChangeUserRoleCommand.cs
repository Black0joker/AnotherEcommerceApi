using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, string Role) : ICommand<AdminUserDto>;
