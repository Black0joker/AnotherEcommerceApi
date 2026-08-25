using ECommerce.Application.Features.Reports.GetCustomerSummary;
using ECommerce.Application.Features.Reports.GetLowStock;
using ECommerce.Application.Features.Reports.GetSalesSummary;
using ECommerce.Application.Features.Reports.GetTopProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sales summary (order count, revenue, average order value, discounts) for a date range.
    /// </summary>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetSalesSummaryQuery(from, to));

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Top products by revenue for a date range.
    /// </summary>
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int count = 10)
    {
        var result = await _mediator.Send(new GetTopProductsQuery(from, to, count));

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Low stock items at or below a threshold.
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 10, [FromQuery] int count = 20)
    {
        var result = await _mediator.Send(new GetLowStockQuery(threshold, count));

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Customer summary (total, new in range, active, inactive).
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomerSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetCustomerSummaryQuery(from, to));

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }
}
