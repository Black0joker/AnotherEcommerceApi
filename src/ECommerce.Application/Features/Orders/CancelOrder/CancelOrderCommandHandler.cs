using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.CancelOrder;

public class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<bool>(Error.Unauthorized(
                "Orders.Unauthorized",
                "You must be authenticated to cancel orders."));
        }

        var order = await _orderRepository.GetWithItemsByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Order.NotFound",
                $"Order with ID '{request.OrderId}' was not found."));
        }

        // IDOR protection
        if (order.UserId != userId.Value)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Order.NotFound",
                $"Order with ID '{request.OrderId}' was not found."));
        }

        if (!order.CanCancel)
        {
            return Result.Failure<bool>(Error.Validation(
                "Order.CannotCancel",
                $"Order cannot be cancelled in '{order.Status}' status. Only Pending or Confirmed orders can be cancelled."));
        }

        // Cancel the order
        order.Cancel(request.Reason);

        // Release reserved inventory
        foreach (var item in order.Items)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId, cancellationToken);
            if (inventory is not null)
            {
                inventory.Release(item.Quantity);

                var transaction = new Domain.Entities.InventoryTransaction
                {
                    InventoryItemId = inventory.Id,
                    ProductId = item.ProductId,
                    Type = InventoryTransactionType.Release,
                    Quantity = item.Quantity,
                    Reference = $"Order Cancelled: {order.OrderNumber}",
                    Notes = request.Reason
                };

                await _inventoryRepository.AddTransactionAsync(transaction, cancellationToken);
                _inventoryRepository.Update(inventory);
            }
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
