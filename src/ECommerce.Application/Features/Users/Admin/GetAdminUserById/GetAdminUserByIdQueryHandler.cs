using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.GetAdminUserById;

public class GetAdminUserByIdQueryHandler : IQueryHandler<GetAdminUserByIdQuery, AdminUserDto>
{
    private readonly IUserRepository _userRepository;

    public GetAdminUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<AdminUserDto>> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AdminUserDto>(Error.NotFound(
                "User.NotFound",
                $"User with ID '{request.Id}' was not found."));
        }

        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        return Result.Success(AdminUsersMapper.ToDto(user, roles));
    }
}
