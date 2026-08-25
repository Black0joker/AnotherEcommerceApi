using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Refresh;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);

        if (!result.Success)
        {
            return Result.Failure<AuthResponse>(Error.Validation(
                "Auth.InvalidRefreshToken",
                result.Error ?? "Invalid refresh token."));
        }

        return Result.Success(new AuthResponse(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value));
    }
}
