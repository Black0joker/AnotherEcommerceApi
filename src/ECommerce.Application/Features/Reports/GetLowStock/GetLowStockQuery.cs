using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetLowStock;

public record GetLowStockQuery(int Threshold = 10, int Count = 20) : IQuery<IReadOnlyList<LowStockDto>>;
