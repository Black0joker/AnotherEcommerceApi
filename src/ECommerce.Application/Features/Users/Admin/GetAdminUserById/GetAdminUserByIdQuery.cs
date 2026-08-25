using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.GetAdminUserById;

public record GetAdminUserByIdQuery(Guid Id) : IQuery<AdminUserDto>;
