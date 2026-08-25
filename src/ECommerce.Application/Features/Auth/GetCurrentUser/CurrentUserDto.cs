namespace ECommerce.Application.Features.Auth.GetCurrentUser;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles
);
