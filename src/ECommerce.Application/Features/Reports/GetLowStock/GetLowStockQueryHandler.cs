using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetLowStock;

public class GetLowStockQueryHandler : IQueryHandler<GetLowStockQuery, IReadOnlyList<LowStockDto>>
{
    private readonly IReportRepository _reportRepository;

    public GetLowStockQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<IReadOnlyList<LowStockDto>>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
    {
        var threshold = request.Threshold < 0 ? 0 : request.Threshold;
        var count = request.Count switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.Count
        };

        var lowStock = await _reportRepository.GetLowStockAsync(threshold, count, cancellationToken);
        return Result.Success(lowStock);
    }
}
