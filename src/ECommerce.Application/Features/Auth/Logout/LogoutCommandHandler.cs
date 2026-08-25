using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Logout;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IAuthService _authService;

    public LogoutCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var success = await _authService.RevokeTokenAsync(request.RefreshToken);

        if (!success)
        {
            return Result.Failure(Error.NotFound(
                "Auth.TokenNotFound",
                "The provided refresh token was not found or already revoked."));
        }

        return Result.Success();
    }
}
