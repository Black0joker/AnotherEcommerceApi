namespace ECommerce.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
}

public record AuthResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null,
    string? Error = null
);
