using ECommerce.Application.Features.Users.Admin.ChangeUserRole;
using ECommerce.Application.Features.Users.Admin.GetAdminUserById;
using ECommerce.Application.Features.Users.Admin.GetAdminUsers;
using ECommerce.Application.Features.Users.Admin.SetUserActive;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List users with pagination, search, and filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? role = null)
    {
        var result = await _mediator.Send(new GetAdminUsersQuery(pageNumber, pageSize, search, isActive, role));

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a specific user by ID.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var result = await _mediator.Send(new GetAdminUserByIdQuery(userId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Activate or deactivate a user.
    /// </summary>
    [HttpPut("{userId:guid}/active")]
    public async Task<IActionResult> SetUserActive(Guid userId, [FromBody] SetUserActiveRequest request)
    {
        var result = await _mediator.Send(new SetUserActiveCommand(userId, request.IsActive));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Change a user's role.
    /// </summary>
    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeUserRoleRequest request)
    {
        var result = await _mediator.Send(new ChangeUserRoleCommand(userId, request.Role));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }
}

public record SetUserActiveRequest(bool IsActive);

public record ChangeUserRoleRequest(string Role);
