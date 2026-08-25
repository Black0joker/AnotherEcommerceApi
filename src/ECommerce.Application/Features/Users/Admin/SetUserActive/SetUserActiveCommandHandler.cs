using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Users.Admin.SetUserActive;

public class SetUserActiveCommandHandler : ICommandHandler<SetUserActiveCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserActiveCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminUserDto>> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AdminUserDto>(Error.NotFound(
                "User.NotFound",
                $"User with ID '{request.UserId}' was not found."));
        }

        if (user.IsActive == request.IsActive)
        {
            return Result.Failure<AdminUserDto>(Error.Validation(
                "User.AlreadyInState",
                $"User is already {(request.IsActive ? "active" : "inactive")}."));
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        return Result.Success(AdminUsersMapper.ToDto(user, roles));
    }
}
