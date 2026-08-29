using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .Include(p => p.Reviews)
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .FirstOrDefaultAsync(p => p.SKU == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Product>();
        }

        // Batch load for checkout-style flows: only category links are needed
        // (discount restrictions). Deliberately no Reviews/OrderItems/InventoryItem
        // includes to keep the query light.
        return await _context.Products
            .Where(p => ids.Contains(p.Id))
            .Include(p => p.ProductCategories)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Products, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        double? minRating,
        double? maxRating,
        bool? isAvailable,
        string? sortBy,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .Include(p => p.Reviews)
            .AsQueryable();

        // Only active products for public queries
        query = query.Where(p => p.IsActive);

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                p.SKU.ToLower().Contains(searchLower));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));
        }

        // Filter by price
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Filter by rating
        if (minRating.HasValue)
        {
            query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= minRating.Value);
        }

        if (maxRating.HasValue)
        {
            query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) <= maxRating.Value);
        }

        // Filter by availability
        if (isAvailable.HasValue && isAvailable.Value)
        {
            query = query.Where(p => p.InventoryItem != null && p.InventoryItem.AvailableQuantity > 0);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products.AnyAsync(p => p.SKU == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(
        Guid productId,
        List<Guid> categoryIds,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (categoryIds.Count == 0)
        {
            // If no categories, just return random active products
            return await _context.Products
                .Where(p => p.IsActive && p.Id != productId)
                .OrderBy(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        return await _context.Products
            .Where(p => p.IsActive && p.Id != productId)
            .Where(p => p.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)))
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(entity, cancellationToken);
    }

    public void Update(Product entity)
    {
        _context.Products.Update(entity);
    }

    public void Delete(Product entity)
    {
        _context.Products.Remove(entity);
    }
}
