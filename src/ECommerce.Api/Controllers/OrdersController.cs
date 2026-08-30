using ECommerce.Application.Features.Orders.Admin.GetAllOrders;
using ECommerce.Application.Features.Orders.Admin.UpdateOrderStatus;
using ECommerce.Application.Features.Orders.CancelOrder;
using ECommerce.Application.Features.Orders.Checkout;
using ECommerce.Application.Features.Orders.GetOrderById;
using ECommerce.Application.Features.Orders.GetUserOrders;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all orders for the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserOrders([FromQuery] GetUserOrdersQuery query)
    {
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a specific order by ID (must belong to current user).
    /// </summary>
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(orderId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            if (result.Error.Code.Contains("Unauthorized"))
                return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Checkout - creates an order from the current cart.
    /// Supports idempotency via Idempotency-Key header.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();

        var shippingAddress = new Application.Features.Orders.Checkout.ShippingAddressDto(
            request.ShippingAddress.FirstName,
            request.ShippingAddress.LastName,
            request.ShippingAddress.StreetLine1,
            request.ShippingAddress.StreetLine2,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.PhoneNumber);

        var command = new CheckoutCommand(idempotencyKey, shippingAddress, request.DiscountCode);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("Unauthorized"))
                return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });

            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancel an order (only Pending or Confirmed orders).
    /// </summary>
    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest? request)
    {
        var command = new CancelOrderCommand(orderId, request?.Reason);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Order cancelled successfully." });
    }
}

/// <summary>
/// Admin endpoints for order management.
/// </summary>
[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all orders (admin only).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllOrders([FromQuery] GetAllOrdersQuery query)
    {
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a specific order by ID (admin only).
    /// </summary>
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(orderId));

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update order status (admin only).
    /// </summary>
    [HttpPut("{orderId:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateStatusRequest request)
    {
        var command = new UpdateOrderStatusCommand(orderId, request.Status);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error!.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Order status updated successfully." });
    }
}

public record CheckoutRequest(CheckoutShippingAddress ShippingAddress, string? DiscountCode = null);

public record CheckoutShippingAddress(
    string FirstName,
    string LastName,
    string StreetLine1,
    string? StreetLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? PhoneNumber
);

public record CancelOrderRequest(string? Reason);

public record UpdateStatusRequest(OrderStatus Status);
