using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetTopProducts;

public record GetTopProductsQuery(DateTime From, DateTime To, int Count = 10) : IQuery<IReadOnlyList<TopProductDto>>;
