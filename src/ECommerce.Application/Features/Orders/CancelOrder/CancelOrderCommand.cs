using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string? Reason) : ICommand<bool>;
