using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.Checkout;

public record CheckoutCommand(
    string? IdempotencyKey,
    ShippingAddressDto ShippingAddress
) : ICommand<CheckoutResultDto>;

public record ShippingAddressDto(
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
