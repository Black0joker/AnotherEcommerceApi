using ECommerce.Application.Features.Reviews.CreateReview;
using ECommerce.Application.Features.Reviews.DeleteReview;
using ECommerce.Application.Features.Reviews.GetProductReviews;
using ECommerce.Application.Features.Reviews.UpdateReview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all approved reviews for a product.
    /// </summary>
    [HttpGet("products/{productId:guid}/reviews")]
    public async Task<IActionResult> GetProductReviews(Guid productId, [FromQuery] GetProductReviewsQuery query)
    {
        query.ProductId = productId;
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a review for a product (requires prior purchase).
    /// </summary>
    [HttpPost("products/{productId:guid}/reviews")]
    [Authorize]
    [EnableRateLimiting("reviews")]
    public async Task<IActionResult> CreateReview(Guid productId, [FromBody] CreateReviewRequest request)
    {
        var command = new CreateReviewCommand(
            productId,
            request.Rating,
            request.Title,
            request.Comment);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            if (result.Error.Code.Contains("Unauthorized"))
                return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return CreatedAtAction(nameof(GetProductReviews), new { productId }, result.Value);
    }

    /// <summary>
    /// Update a review (only the review owner).
    /// </summary>
    [HttpPut("reviews/{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewRequest request)
    {
        var command = new UpdateReviewCommand(
            reviewId,
            request.Rating,
            request.Title,
            request.Comment);

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
    /// Delete a review (only the review owner or admin).
    /// </summary>
    [HttpDelete("reviews/{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        var command = new DeleteReviewCommand(reviewId);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return NoContent();
    }
}

public record CreateReviewRequest(int Rating, string? Title, string? Comment);
public record UpdateReviewRequest(int Rating, string? Title, string? Comment);
