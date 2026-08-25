using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetTopProducts;

public class GetTopProductsQueryHandler : IQueryHandler<GetTopProductsQuery, IReadOnlyList<TopProductDto>>
{
    private readonly IReportRepository _reportRepository;

    public GetTopProductsQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<IReadOnlyList<TopProductDto>>> Handle(GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            return Result.Failure<IReadOnlyList<TopProductDto>>(Error.Validation(
                "Report.InvalidDateRange",
                "The 'From' date must be before the 'To' date."));
        }

        var count = request.Count switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => request.Count
        };

        var topProducts = await _reportRepository.GetTopProductsAsync(request.From, request.To, count, cancellationToken);
        return Result.Success(topProducts);
    }
}
