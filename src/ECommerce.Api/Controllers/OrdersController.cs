using ECommerce.Application.Features.Orders.Checkout;
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
    /// Checkout - creates an order from the current cart.
    /// Supports idempotency via Idempotency-Key header.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();

        var shippingAddress = new ShippingAddressDto(
            request.ShippingAddress.FirstName,
            request.ShippingAddress.LastName,
            request.ShippingAddress.StreetLine1,
            request.ShippingAddress.StreetLine2,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.PhoneNumber);

        var command = new CheckoutCommand(idempotencyKey, shippingAddress);
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
}

public record CheckoutRequest(CheckoutShippingAddress ShippingAddress);

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
