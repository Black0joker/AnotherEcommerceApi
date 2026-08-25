using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.GetAdminUsers;

public record GetAdminUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    string? Role = null) : IQuery<GetAdminUsersResult>;

public record GetAdminUsersResult(
    IReadOnlyList<AdminUserDto> Users,
    int TotalCount,
    int PageNumber,
    int PageSize);
