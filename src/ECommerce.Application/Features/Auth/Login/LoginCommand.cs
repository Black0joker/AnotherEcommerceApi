using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Login;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthResponse>;
