namespace ECommerce.Application.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}
