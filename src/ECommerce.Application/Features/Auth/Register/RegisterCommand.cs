using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : ICommand<AuthResponse>;
