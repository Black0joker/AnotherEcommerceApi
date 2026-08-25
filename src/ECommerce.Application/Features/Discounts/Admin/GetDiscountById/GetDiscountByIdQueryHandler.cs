using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Discounts.Admin.GetDiscountById;

public class GetDiscountByIdQueryHandler : IQueryHandler<GetDiscountByIdQuery, DiscountDto>
{
    private readonly IDiscountRepository _discountRepository;

    public GetDiscountByIdQueryHandler(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<Result<DiscountDto>> Handle(GetDiscountByIdQuery request, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);

        if (discount is null)
        {
            return Result.Failure<DiscountDto>(Error.NotFound(
                "Discount.NotFound",
                $"Discount with ID '{request.Id}' was not found."));
        }

        return Result.Success(DiscountMapper.ToDto(discount));
    }
}
