using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Login;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            return Result.Failure<AuthResponse>(Error.Validation(
                "Auth.InvalidCredentials",
                result.Error ?? "Invalid email or password."));
        }

        return Result.Success(new AuthResponse(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value));
    }
}
