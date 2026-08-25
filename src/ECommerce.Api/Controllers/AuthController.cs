using ECommerce.Application.Features.Auth;
using ECommerce.Application.Features.Auth.GetCurrentUser;
using ECommerce.Application.Features.Auth.Login;
using ECommerce.Application.Features.Auth.Logout;
using ECommerce.Application.Features.Auth.Refresh;
using ECommerce.Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return CreatedAtAction(nameof(Register), new AuthResponse(
            result.Value.AccessToken,
            result.Value.RefreshToken,
            result.Value.ExpiresAt));
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(new AuthResponse(
            result.Value.AccessToken,
            result.Value.RefreshToken,
            result.Value.ExpiresAt));
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(new AuthResponse(
            result.Value.AccessToken,
            result.Value.RefreshToken,
            result.Value.ExpiresAt));
    }

    /// <summary>
    /// Logout and revoke refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _mediator.Send(new GetCurrentUserQuery());

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }
}
