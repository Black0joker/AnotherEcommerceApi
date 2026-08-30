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
        // Lightweight single-table load for existence checks and flows that
        // only need scalar product fields. Navigations are intentionally not
        // included: loading Reviews/OrderItems here used to pull the entire
        // review and order-item history on every call (e.g. add-to-cart).
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Read-side detail graph. OrderItems are deliberately excluded:
        // order history is never needed by the detail endpoint and would
        // load one row per unit ever sold for popular products.
        return await _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> HasOrderItemsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .AnyAsync(oi => oi.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Pure read (write paths load by id/slug): skip change tracking.
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailRead?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        // Single no-tracking projection (also served from cache): no entity
        // graph, stock and rating read from persisted aggregates.
        return await _context.Products
            .Where(p => p.Slug == slug)
            .Select(p => new ProductDetailRead(
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                p.SKU,
                p.Price,
                p.CompareAtPrice,
                p.IsActive,
                p.InventoryItem != null ? p.InventoryItem.AvailableQuantity : 0,
                p.AverageRating,
                p.RatingCount,
                p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        // Pure read: skip change tracking.
        return await _context.Products
            .AsNoTracking()
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
        // includes to keep the query light. Callers only read price/stock;
        // inventory rows are loaded and mutated separately.
        return await _context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Include(p => p.ProductCategories)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        // EF state change is synchronous; the async surface exists so
        // decorators can attach async side effects (cache invalidation).
        _context.Products.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(entity);
        return Task.CompletedTask;
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
        // No Reviews include: rating filtering/sorting uses the persisted
        // AverageRating/RatingCount aggregates on Product. Listing is a pure
        // read, so skip change tracking.
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.ProductCategories)
            .Include(p => p.InventoryItem)
            .AsQueryable();

        // Only active products for public queries
        query = query.Where(p => p.IsActive);

        // Search. Plain Contains: wrapping the columns in LOWER() is
        // non-sargable and defeats IX_Products_Name; SQL Server's default
        // collation is case-insensitive, so no lowercasing is needed.
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)) ||
                p.SKU.Contains(search));
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

        // Filter by rating using the persisted approved-review aggregates
        // (indexed on Product) instead of a correlated Reviews.Average
        // subquery evaluated per row.
        if (minRating.HasValue)
        {
            query = query.Where(p => p.RatingCount > 0 && p.AverageRating >= minRating.Value);
        }

        if (maxRating.HasValue)
        {
            query = query.Where(p => p.RatingCount > 0 && p.AverageRating <= maxRating.Value);
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
            "rating" => query.OrderByDescending(p => p.AverageRating),
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

    public async Task<ProductDetailRead?> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Single no-tracking projection for the detail page: scalar fields,
        // available stock and the persisted rating aggregates. Replaces the
        // heavy include graph plus the separate average-rating round-trip.
        return await _context.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductDetailRead(
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                p.SKU,
                p.Price,
                p.CompareAtPrice,
                p.IsActive,
                p.InventoryItem != null ? p.InventoryItem.AvailableQuantity : 0,
                p.AverageRating,
                p.RatingCount,
                p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Guid>?> GetCategoryIdsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Id == productId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await _context.ProductCategories
            .Where(pc => pc.ProductId == productId)
            .Select(pc => pc.CategoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RelatedProductRead>> GetRelatedProductsAsync(
        Guid productId,
        List<Guid> categoryIds,
        int count,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Where(p => p.IsActive && p.Id != productId);

        if (categoryIds.Count > 0)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)));
        }

        // Server-side projection: only display fields are materialized and
        // ratings come from the persisted aggregates, so the cached payload
        // is a flat list instead of an entity graph.
        return await query
            .OrderBy(p => p.CreatedAt)
            .Take(count)
            .Select(p => new RelatedProductRead(
                p.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.CompareAtPrice,
                p.AverageRating))
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
