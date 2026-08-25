using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetCustomerSummary;

public class GetCustomerSummaryQueryHandler : IQueryHandler<GetCustomerSummaryQuery, CustomerSummaryDto>
{
    private readonly IReportRepository _reportRepository;

    public GetCustomerSummaryQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<CustomerSummaryDto>> Handle(GetCustomerSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            return Result.Failure<CustomerSummaryDto>(Error.Validation(
                "Report.InvalidDateRange",
                "The 'From' date must be before the 'To' date."));
        }

        var summary = await _reportRepository.GetCustomerSummaryAsync(request.From, request.To, cancellationToken);
        return Result.Success(summary);
    }
}
