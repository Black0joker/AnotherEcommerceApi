using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailDto>;
