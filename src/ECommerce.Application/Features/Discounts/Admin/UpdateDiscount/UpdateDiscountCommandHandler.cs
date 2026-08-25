using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Discounts.Admin.UpdateDiscount;

public class UpdateDiscountCommandHandler : ICommandHandler<UpdateDiscountCommand, DiscountDto>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDiscountCommandHandler(IDiscountRepository discountRepository, IUnitOfWork unitOfWork)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DiscountDto>> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);

        if (discount is null)
        {
            return Result.Failure<DiscountDto>(Error.NotFound(
                "Discount.NotFound",
                $"Discount with ID '{request.Id}' was not found."));
        }

        if (request.Value <= 0)
        {
            return Result.Failure<DiscountDto>(Error.Validation(
                "Discount.InvalidValue",
                "Discount value must be greater than zero."));
        }

        discount.Name = request.Name;
        discount.Description = request.Description;
        discount.Type = request.Type;
        discount.Value = request.Value;
        discount.StartsAt = request.StartsAt;
        discount.EndsAt = request.EndsAt;
        discount.UsageLimit = request.UsageLimit;
        discount.MinimumOrderValue = request.MinimumOrderValue;
        discount.IsActive = request.IsActive;

        // Replace product restrictions.
        discount.DiscountProducts.Clear();
        if (request.ProductIds is { Count: > 0 })
        {
            foreach (var productId in request.ProductIds.Distinct())
            {
                discount.DiscountProducts.Add(new DiscountProduct { ProductId = productId });
            }
        }

        // Replace category restrictions.
        discount.DiscountCategories.Clear();
        if (request.CategoryIds is { Count: > 0 })
        {
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                discount.DiscountCategories.Add(new DiscountCategory { CategoryId = categoryId });
            }
        }

        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(DiscountMapper.ToDto(discount));
    }
}
