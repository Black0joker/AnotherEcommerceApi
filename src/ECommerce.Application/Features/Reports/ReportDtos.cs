namespace ECommerce.Application.Features.Reports;

public record SalesSummaryDto(
    DateTime From,
    DateTime To,
    int OrderCount,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    decimal TotalDiscountGiven);

public record TopProductDto(
    Guid ProductId,
    string ProductName,
    string? Sku,
    int UnitsSold,
    decimal Revenue);

public record LowStockDto(
    Guid ProductId,
    string ProductName,
    string? Sku,
    int AvailableQuantity,
    int ReservedQuantity);

public record CustomerSummaryDto(
    int TotalCustomers,
    int NewCustomersInRange,
    int ActiveCustomers,
    int InactiveCustomers);
