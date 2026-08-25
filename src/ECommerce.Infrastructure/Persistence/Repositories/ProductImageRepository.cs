using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class ProductImageRepository : IProductImageRepository
{
    private readonly ApplicationDbContext _context;

    public ProductImageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductImage?> GetPrimaryByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.IsPrimary, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductImage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductImage entity, CancellationToken cancellationToken = default)
    {
        await _context.ProductImages.AddAsync(entity, cancellationToken);
    }

    public void Update(ProductImage entity)
    {
        _context.ProductImages.Update(entity);
    }

    public void Delete(ProductImage entity)
    {
        _context.ProductImages.Remove(entity);
    }
}
