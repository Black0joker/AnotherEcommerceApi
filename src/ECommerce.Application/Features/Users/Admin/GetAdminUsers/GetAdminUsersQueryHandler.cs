using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.GetAdminUsers;

public class GetAdminUsersQueryHandler : IQueryHandler<GetAdminUsersQuery, GetAdminUsersResult>
{
    private readonly IUserRepository _userRepository;

    public GetAdminUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetAdminUsersResult>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        var (users, totalCount) = await _userRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.Search,
            request.IsActive,
            request.Role,
            cancellationToken);

        // One batch query for all roles on the page instead of ~2 round-trips
        // per user (previously up to 200+ extra queries for a 100-user page).
        var userIds = users.Select(u => u.Id).ToList();
        var rolesByUserId = await _userRepository.GetRolesByUserIdsAsync(userIds, cancellationToken);

        var dtos = users
            .Select(user => AdminUsersMapper.ToDto(
                user,
                rolesByUserId.TryGetValue(user.Id, out var roles) ? roles : Array.Empty<string>()))
            .ToList();

        return Result.Success(new GetAdminUsersResult(dtos, totalCount, pageNumber, pageSize));
    }
}
