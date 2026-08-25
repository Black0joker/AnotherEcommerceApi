using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return new AuthResult(false, Error: "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new AuthResult(false, Error: errors);
        }

        // Assign Customer role by default
        await _userManager.AddToRoleAsync(user, "Customer");

        return await GenerateTokensAsync(user);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return new AuthResult(false, Error: "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return new AuthResult(false, Error: "Account is deactivated.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);

        if (!passwordValid)
        {
            return new AuthResult(false, Error: "Invalid email or password.");
        }

        return await GenerateTokensAsync(user);
    }

    public async Task<AuthResult> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var userId = _tokenService.ValidateAccessToken(accessToken);

        // Even if the access token is expired, we can still validate the user
        // by checking the refresh token
        if (userId is null)
        {
            // Try to extract user from an expired token
            userId = ExtractUserIdFromExpiredToken(accessToken);
        }

        if (userId is null)
        {
            return new AuthResult(false, Error: "Invalid token.");
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null || !user.IsActive)
        {
            return new AuthResult(false, Error: "User not found or deactivated.");
        }

        // Check if refresh token exists in the user's tokens
        var existingToken = user.RefreshTokens
            .FirstOrDefault(rt => rt.Token == refreshToken && rt.RevokedAt == null);

        if (existingToken is null)
        {
            return new AuthResult(false, Error: "Invalid refresh token.");
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return new AuthResult(false, Error: "Refresh token has expired.");
        }

        // Rotate: revoke old token, issue new one
        existingToken.RevokedAt = DateTime.UtcNow;

        return await GenerateTokensAsync(user);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        // Find the user who owns this refresh token
        // In a real implementation, you'd query by token
        // For now, we'll need to search through users
        // This is a simplified approach - in production, you'd have a dedicated token store
        return await Task.FromResult(true);
    }

    private async Task<AuthResult> GenerateTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Store refresh token
        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        });

        await _userManager.UpdateAsync(user);

        return new AuthResult(true, accessToken, refreshToken, expiresAt);
    }

    private static Guid? ExtractUserIdFromExpiredToken(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            return sub is not null ? Guid.Parse(sub) : null;
        }
        catch
        {
            return null;
        }
    }
}
