using ECommerce.Application.Features.Wishlist.AddToWishlist;
using ECommerce.Application.Features.Wishlist.GetWishlist;
using ECommerce.Application.Features.Wishlist.RemoveFromWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the current user's wishlist.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var result = await _mediator.Send(new GetWishlistQuery());

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Add a product to the wishlist.
    /// </summary>
    [HttpPost("items/{productId:guid}")]
    public async Task<IActionResult> AddToWishlist(Guid productId)
    {
        var result = await _mediator.Send(new AddToWishlistCommand(productId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            if (result.Error.Code.Contains("AlreadyExists"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Remove a product from the wishlist.
    /// </summary>
    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId)
    {
        var result = await _mediator.Send(new RemoveFromWishlistCommand(productId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }
}
