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

        var dtos = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
            dtos.Add(AdminUsersMapper.ToDto(user, roles));
        }

        return Result.Success(new GetAdminUsersResult(dtos, totalCount, pageNumber, pageSize));
    }
}
