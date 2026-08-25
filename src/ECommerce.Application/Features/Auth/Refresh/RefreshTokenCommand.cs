using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Refresh;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : ICommand<AuthResponse>;
