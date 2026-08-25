using ECommerce.Application.Features.Reports;

namespace ECommerce.Application.Abstractions;

public interface IReportRepository
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockDto>> GetLowStockAsync(int threshold, int count, CancellationToken cancellationToken = default);
    Task<CustomerSummaryDto> GetCustomerSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
