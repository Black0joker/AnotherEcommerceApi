using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Discounts.Admin.GetDiscounts;

public class GetDiscountsQueryHandler : IQueryHandler<GetDiscountsQuery, IReadOnlyList<DiscountDto>>
{
    private readonly IDiscountRepository _discountRepository;

    public GetDiscountsQueryHandler(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<Result<IReadOnlyList<DiscountDto>>> Handle(GetDiscountsQuery request, CancellationToken cancellationToken)
    {
        var discounts = await _discountRepository.GetAllAsync(cancellationToken);
        var dtos = discounts.Select(DiscountMapper.ToDto).ToList();
        return Result.Success<IReadOnlyList<DiscountDto>>(dtos);
    }
}
