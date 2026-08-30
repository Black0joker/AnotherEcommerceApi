using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Review?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Review> Reviews, int TotalCount)> GetApprovedByProductIdPagedAsync(
        Guid productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // The IsApproved filter lives in SQL instead of client-side filtering,
        // and only the requested page is materialized.
        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.IsApproved);

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reviews, totalCount);
    }

    public async Task<bool> HasUserPurchasedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .AnyAsync(oi => oi.Order.UserId == userId
                && oi.ProductId == productId
                && oi.Order.Status != Domain.Enums.OrderStatus.Cancelled,
                cancellationToken);
    }

    public async Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .AnyAsync(r => r.UserId == userId && r.ProductId == productId, cancellationToken);
    }

    public async Task<double> GetAverageRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .AverageAsync(r => (double?)r.Rating, cancellationToken) ?? 0;
    }

    public async Task<int> GetReviewCountAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .CountAsync(r => r.ProductId == productId && r.IsApproved, cancellationToken);
    }

    public async Task RecalculateProductRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return;
        }

        product.RatingCount = await _context.Reviews
            .CountAsync(r => r.ProductId == productId && r.IsApproved, cancellationToken);

        product.AverageRating = await _context.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .AverageAsync(r => (double?)r.Rating, cancellationToken) ?? 0;
    }

    public async Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Review entity, CancellationToken cancellationToken = default)
    {
        await _context.Reviews.AddAsync(entity, cancellationToken);
    }

    public void Update(Review entity)
    {
        _context.Reviews.Update(entity);
    }

    public void Delete(Review entity)
    {
        _context.Reviews.Remove(entity);
    }
}
