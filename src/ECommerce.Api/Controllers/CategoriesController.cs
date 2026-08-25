using ECommerce.Application.Features.Categories.CreateCategory;
using ECommerce.Application.Features.Categories.DeleteCategory;
using ECommerce.Application.Features.Categories.GetCategories;
using ECommerce.Application.Features.Categories.GetCategory;
using ECommerce.Application.Features.Categories.UpdateCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active categories.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a category by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryQuery(id));

        if (result.IsFailure)
            return NotFound(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new category. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return CreatedAtAction(nameof(GetCategory), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>
    /// Update an existing category. Admin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { error = "Validation.IdMismatch", message = "Category ID in URL does not match command ID." });

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
    /// Delete a category. Admin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return NoContent();
    }
}
