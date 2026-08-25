using ECommerce.Application.Features.Discounts;
using ECommerce.Application.Features.Discounts.Admin.CreateDiscount;
using ECommerce.Application.Features.Discounts.Admin.DeleteDiscount;
using ECommerce.Application.Features.Discounts.Admin.GetDiscountById;
using ECommerce.Application.Features.Discounts.Admin.GetDiscounts;
using ECommerce.Application.Features.Discounts.Admin.UpdateDiscount;
using ECommerce.Application.Features.Discounts.ValidateDiscountCode;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Customer-facing discount endpoints.
/// </summary>
[ApiController]
[Route("api/v1/discounts")]
[Authorize]
public class DiscountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Validate a discount code against the current cart and preview the price breakdown.
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateDiscountCode([FromBody] ValidateDiscountCodeRequest request)
    {
        var result = await _mediator.Send(new ValidateDiscountCodeQuery(request.Code));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("Unauthorized"))
                return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }
}

/// <summary>
/// Admin endpoints for discount management.
/// </summary>
[ApiController]
[Route("api/v1/admin/discounts")]
[Authorize(Roles = "Admin")]
public class AdminDiscountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDiscountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all discounts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDiscounts()
    {
        var result = await _mediator.Send(new GetDiscountsQuery());

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a discount by ID.
    /// </summary>
    [HttpGet("{discountId:guid}")]
    public async Task<IActionResult> GetDiscountById(Guid discountId)
    {
        var result = await _mediator.Send(new GetDiscountByIdQuery(discountId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new discount.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request)
    {
        var command = new CreateDiscountCommand(
            request.Code,
            request.Name,
            request.Description,
            request.Type,
            request.Value,
            request.StartsAt,
            request.EndsAt,
            request.UsageLimit,
            request.MinimumOrderValue,
            request.IsActive,
            request.ProductIds ?? Array.Empty<Guid>(),
            request.CategoryIds ?? Array.Empty<Guid>());

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("Conflict"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return CreatedAtAction(nameof(GetDiscountById), new { discountId = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing discount.
    /// </summary>
    [HttpPut("{discountId:guid}")]
    public async Task<IActionResult> UpdateDiscount(Guid discountId, [FromBody] UpdateDiscountRequest request)
    {
        var command = new UpdateDiscountCommand(
            discountId,
            request.Name,
            request.Description,
            request.Type,
            request.Value,
            request.StartsAt,
            request.EndsAt,
            request.UsageLimit,
            request.MinimumOrderValue,
            request.IsActive,
            request.ProductIds ?? Array.Empty<Guid>(),
            request.CategoryIds ?? Array.Empty<Guid>());

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Deactivate a discount (soft delete to preserve order history).
    /// </summary>
    [HttpDelete("{discountId:guid}")]
    public async Task<IActionResult> DeleteDiscount(Guid discountId)
    {
        var result = await _mediator.Send(new DeleteDiscountCommand(discountId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Discount deactivated successfully." });
    }
}

public record ValidateDiscountCodeRequest(string Code);

public record CreateDiscountRequest(
    string Code,
    string Name,
    string? Description,
    DiscountType Type,
    decimal Value,
    DateTime StartsAt,
    DateTime? EndsAt,
    int? UsageLimit,
    decimal? MinimumOrderValue,
    bool IsActive,
    IReadOnlyList<Guid>? ProductIds,
    IReadOnlyList<Guid>? CategoryIds);

public record UpdateDiscountRequest(
    string Name,
    string? Description,
    DiscountType Type,
    decimal Value,
    DateTime StartsAt,
    DateTime? EndsAt,
    int? UsageLimit,
    decimal? MinimumOrderValue,
    bool IsActive,
    IReadOnlyList<Guid>? ProductIds,
    IReadOnlyList<Guid>? CategoryIds);
