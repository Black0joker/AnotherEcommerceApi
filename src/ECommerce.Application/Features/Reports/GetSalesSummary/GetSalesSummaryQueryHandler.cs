using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetSalesSummary;

public class GetSalesSummaryQueryHandler : IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    private readonly IReportRepository _reportRepository;

    public GetSalesSummaryQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<SalesSummaryDto>> Handle(GetSalesSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            return Result.Failure<SalesSummaryDto>(Error.Validation(
                "Report.InvalidDateRange",
                "The 'From' date must be before the 'To' date."));
        }

        var summary = await _reportRepository.GetSalesSummaryAsync(request.From, request.To, cancellationToken);
        return Result.Success(summary);
    }
}
