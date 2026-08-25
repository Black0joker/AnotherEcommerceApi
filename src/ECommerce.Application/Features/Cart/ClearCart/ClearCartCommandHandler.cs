using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Cart.ClearCart;

public class ClearCartCommandHandler : ICommandHandler<ClearCartCommand>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ClearCartCommandHandler(
        ICartRepository cartRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure(Error.Unauthorized(
                "Cart.Unauthorized",
                "You must be authenticated to manage your cart."));
        }

        var cart = await _cartRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        if (cart is null)
        {
            // Cart doesn't exist, nothing to clear
            return Result.Success();
        }

        cart.Items.Clear();
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
