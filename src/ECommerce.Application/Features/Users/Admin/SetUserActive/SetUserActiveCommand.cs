using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.SetUserActive;

public record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand<AdminUserDto>;
