using ECommerce.Application.Features.Inventory.AdjustInventory;
using ECommerce.Application.Features.Inventory.GetInventory;
using ECommerce.Application.Features.Inventory.GetInventoryTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize(Roles = "Admin")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get inventory for a specific product.
    /// </summary>
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetInventory(Guid productId)
    {
        var result = await _mediator.Send(new GetInventoryQuery(productId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Adjust inventory quantity (positive to add, negative to remove).
    /// </summary>
    [HttpPost("{productId:guid}/adjust")]
    public async Task<IActionResult> AdjustInventory(Guid productId, [FromBody] AdjustInventoryRequest request)
    {
        var command = new AdjustInventoryCommand(productId, request.Quantity, request.Reason);
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
    /// Get inventory transaction history (audit trail) for a product.
    /// </summary>
    [HttpGet("{productId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid productId, [FromQuery] GetInventoryTransactionsQuery query)
    {
        query.ProductId = productId;
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }
}

public record AdjustInventoryRequest(int Quantity, string? Reason);
