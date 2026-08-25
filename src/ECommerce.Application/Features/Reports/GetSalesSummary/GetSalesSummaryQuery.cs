using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetSalesSummary;

public record GetSalesSummaryQuery(DateTime From, DateTime To) : IQuery<SalesSummaryDto>;
