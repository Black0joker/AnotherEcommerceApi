using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Discounts.Admin.CreateDiscount;

public class CreateDiscountCommandHandler : ICommandHandler<CreateDiscountCommand, DiscountDto>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDiscountCommandHandler(IDiscountRepository discountRepository, IUnitOfWork unitOfWork)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DiscountDto>> Handle(CreateDiscountCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _discountRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            return Result.Failure<DiscountDto>(Error.Conflict(
                "Discount.CodeAlreadyExists",
                $"A discount with code '{code}' already exists."));
        }

        if (request.Value <= 0)
        {
            return Result.Failure<DiscountDto>(Error.Validation(
                "Discount.InvalidValue",
                "Discount value must be greater than zero."));
        }

        var discount = new Discount
        {
            Code = code,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Value = request.Value,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            UsageLimit = request.UsageLimit,
            MinimumOrderValue = request.MinimumOrderValue,
            IsActive = request.IsActive
        };

        if (request.ProductIds is { Count: > 0 })
        {
            foreach (var productId in request.ProductIds.Distinct())
            {
                discount.DiscountProducts.Add(new DiscountProduct { ProductId = productId });
            }
        }

        if (request.CategoryIds is { Count: > 0 })
        {
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                discount.DiscountCategories.Add(new DiscountCategory { CategoryId = categoryId });
            }
        }

        await _discountRepository.AddAsync(discount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(DiscountMapper.ToDto(discount));
    }
}
