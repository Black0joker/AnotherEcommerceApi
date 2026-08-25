using ECommerce.Application.Features.Uploads.DeleteProductImage;
using ECommerce.Application.Features.Uploads.UploadProductImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UploadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Upload an image for a product.
    /// </summary>
    [HttpPost("products/{productId:guid}/images")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadProductImage(
        Guid productId,
        IFormFile file,
        [FromQuery] bool isPrimary = false)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Upload.NoFile", message = "No file provided." });
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadProductImageCommand(
            productId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            isPrimary);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            if (result.Error.Code.Contains("Unauthorized"))
                return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return CreatedAtAction(nameof(GetProductImages), new { productId }, result.Value);
    }

    /// <summary>
    /// Delete a product image.
    /// </summary>
    [HttpDelete("images/{imageId:guid}")]
    public async Task<IActionResult> DeleteProductImage(Guid imageId)
    {
        var command = new DeleteProductImageCommand(imageId);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return NoContent();
    }

    /// <summary>
    /// Get all images for a product.
    /// </summary>
    [HttpGet("products/{productId:guid}/images")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductImages(Guid productId)
    {
        var query = new ECommerce.Application.Features.Uploads.GetProductImages.GetProductImagesQuery(productId);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }
}
