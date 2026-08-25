using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Orders.Admin.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetWithItemsByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Order.NotFound",
                $"Order with ID '{request.OrderId}' was not found."));
        }

        try
        {
            order.TransitionTo(request.NewStatus);
        }
        catch (Domain.Exceptions.BusinessRuleViolationException ex)
        {
            return Result.Failure<bool>(Error.Validation(
                "Order.InvalidTransition",
                ex.Message));
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
