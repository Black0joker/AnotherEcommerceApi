using ECommerce.Application.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.Admin.UpdateOrderStatus;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : ICommand<bool>;
