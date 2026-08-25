namespace ECommerce.Application.Features.Users.Admin;

public record AdminUserDto(
    Guid Id,
    string? Email,
    string? UserName,
    string? FirstName,
    string? LastName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles);
