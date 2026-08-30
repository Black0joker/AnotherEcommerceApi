using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly ApplicationDbContext _context;

    public WishlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Pure read (write paths load by user id): skip change tracking.
        return await _context.Wishlists
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<Wishlist?> GetWithItemsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Wishlist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Wishlist entity, CancellationToken cancellationToken = default)
    {
        await _context.Wishlists.AddAsync(entity, cancellationToken);
    }

    public void Update(Wishlist entity)
    {
        _context.Wishlists.Update(entity);
    }

    public void Delete(Wishlist entity)
    {
        _context.Wishlists.Remove(entity);
    }
}
