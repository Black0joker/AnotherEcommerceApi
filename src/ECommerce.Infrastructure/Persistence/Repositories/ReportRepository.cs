using ECommerce.Application.Abstractions;
using ECommerce.Application.Features.Reports;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var orders = _context.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Status != OrderStatus.Cancelled);

        // Single aggregate query: count and both sums in one round-trip
        // instead of three sequential queries.
        var stats = await orders
            .GroupBy(o => 1)
            .Select(g => new
            {
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.GrandTotal),
                TotalDiscount = g.Sum(o => o.DiscountAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var orderCount = stats?.OrderCount ?? 0;
        var totalRevenue = stats?.TotalRevenue ?? 0m;
        var totalDiscount = stats?.TotalDiscount ?? 0m;
        var averageOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0m;

        return new SalesSummaryDto(from, to, orderCount, totalRevenue, averageOrderValue, totalDiscount);
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int count, CancellationToken cancellationToken = default)
    {
        var topProducts = await _context.OrderItems
            .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to && oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.SKU })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.SKU,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.Total)))
            .OrderByDescending(p => p.Revenue)
            .Take(count)
            .ToListAsync(cancellationToken);

        return topProducts;
    }

    public async Task<IReadOnlyList<LowStockDto>> GetLowStockAsync(int threshold, int count, CancellationToken cancellationToken = default)
    {
        var lowStock = await _context.InventoryItems
            .Where(i => i.AvailableQuantity <= threshold)
            .OrderBy(i => i.AvailableQuantity)
            .Take(count)
            .Select(i => new LowStockDto(
                i.ProductId,
                i.Product.Name,
                i.Product.SKU,
                i.AvailableQuantity,
                i.ReservedQuantity))
            .ToListAsync(cancellationToken);

        return lowStock;
    }

    public async Task<CustomerSummaryDto> GetCustomerSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // Single aggregate query with conditional counts instead of three
        // separate COUNT round-trips.
        var stats = await _context.Users
            .GroupBy(u => 1)
            .Select(g => new
            {
                TotalCustomers = g.Count(),
                NewCustomersInRange = g.Count(u => u.CreatedAt >= from && u.CreatedAt <= to),
                ActiveCustomers = g.Count(u => u.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalCustomers = stats?.TotalCustomers ?? 0;
        var newCustomersInRange = stats?.NewCustomersInRange ?? 0;
        var activeCustomers = stats?.ActiveCustomers ?? 0;
        var inactiveCustomers = totalCustomers - activeCustomers;

        return new CustomerSummaryDto(totalCustomers, newCustomersInRange, activeCustomers, inactiveCustomers);
    }
}
