using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Reports.GetCustomerSummary;

public record GetCustomerSummaryQuery(DateTime From, DateTime To) : IQuery<CustomerSummaryDto>;
