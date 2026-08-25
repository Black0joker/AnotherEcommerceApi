using ECommerce.Application.Features.Products.CreateProduct;
using ECommerce.Application.Features.Products.DeleteProduct;
using ECommerce.Application.Features.Products.GetProduct;
using ECommerce.Application.Features.Products.GetProducts;
using ECommerce.Application.Features.Products.GetRelatedProducts;
using ECommerce.Application.Features.Products.UpdateProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of products with filtering, sorting, and search.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query)
    {
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a product by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductQuery(id));

        if (result.IsFailure)
            return NotFound(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get related products for a given product.
    /// </summary>
    [HttpGet("{id:guid}/related")]
    public async Task<IActionResult> GetRelatedProducts(Guid id, [FromQuery] int count = 8)
    {
        var result = await _mediator.Send(new GetRelatedProductsQuery(id, count));

        if (result.IsFailure)
            return NotFound(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new product. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>
    /// Update an existing product. Admin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { error = "Validation.IdMismatch", message = "Product ID in URL does not match command ID." });

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
    /// Delete a product. Admin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return NoContent();
    }
}
