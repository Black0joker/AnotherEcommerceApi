using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IDiscountRepository : IRepository<Discount>
{
    Task<Discount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
}
