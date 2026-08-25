using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.ChangeUserRole;

public class ChangeUserRoleCommandHandler : ICommandHandler<ChangeUserRoleCommand, AdminUserDto>
{
    private static readonly string[] AllowedRoles = ["Admin", "Customer"];

    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminUserDto>> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var targetRole = request.Role.Trim();

        if (!AllowedRoles.Contains(targetRole))
        {
            return Result.Failure<AdminUserDto>(Error.Validation(
                "User.InvalidRole",
                $"Role '{targetRole}' is not valid. Allowed roles: {string.Join(", ", AllowedRoles)}."));
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AdminUserDto>(Error.NotFound(
                "User.NotFound",
                $"User with ID '{request.UserId}' was not found."));
        }

        var currentRoles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);

        // Prevent an admin from removing their own Admin role.
        if (_currentUserService.UserId == request.UserId && currentRoles.Contains("Admin") && targetRole != "Admin")
        {
            return Result.Failure<AdminUserDto>(Error.Validation(
                "User.CannotDemoteSelf",
                "You cannot remove your own Admin role."));
        }

        // Prevent removing the last remaining admin.
        if (currentRoles.Contains("Admin") && targetRole != "Admin")
        {
            var (admins, adminCount) = await _userRepository.GetPagedAsync(1, int.MaxValue, null, true, "Admin", cancellationToken);
            if (adminCount <= 1)
            {
                return Result.Failure<AdminUserDto>(Error.Validation(
                    "User.LastAdmin",
                    "Cannot remove the Admin role from the last remaining admin."));
            }
        }

        // Remove all current roles, then add the target role.
        foreach (var role in currentRoles)
        {
            await _userRepository.RemoveFromRoleAsync(user, role, cancellationToken);
        }

        await _userRepository.AddToRoleAsync(user, targetRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedRoles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        return Result.Success(AdminUsersMapper.ToDto(user, updatedRoles));
    }
}
