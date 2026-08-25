using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Discounts.Admin.DeleteDiscount;

public class DeleteDiscountCommandHandler : ICommandHandler<DeleteDiscountCommand, bool>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDiscountCommandHandler(IDiscountRepository discountRepository, IUnitOfWork unitOfWork)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);

        if (discount is null)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Discount.NotFound",
                $"Discount with ID '{request.Id}' was not found."));
        }

        // Deactivate rather than hard-delete so historical orders remain auditable.
        discount.IsActive = false;

        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
