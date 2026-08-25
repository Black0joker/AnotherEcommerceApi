using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Inventory.GetInventoryTransactions;

public class GetInventoryTransactionsQueryHandler : IQueryHandler<GetInventoryTransactionsQuery, IReadOnlyList<InventoryTransactionDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryTransactionsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<IReadOnlyList<InventoryTransactionDto>>> Handle(GetInventoryTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _inventoryRepository.GetTransactionsByProductIdAsync(request.ProductId, cancellationToken);

        var dtos = transactions.Select(t => new InventoryTransactionDto(
            t.Id,
            t.ProductId,
            t.Type,
            t.Quantity,
            t.Reference,
            t.Notes,
            t.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<InventoryTransactionDto>>(dtos);
    }
}
