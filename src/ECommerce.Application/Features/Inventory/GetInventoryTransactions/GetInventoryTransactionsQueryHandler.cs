using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Inventory.GetInventoryTransactions;

public class GetInventoryTransactionsQueryHandler : IQueryHandler<GetInventoryTransactionsQuery, PagedResult<InventoryTransactionDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryTransactionsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<PagedResult<InventoryTransactionDto>>> Handle(GetInventoryTransactionsQuery request, CancellationToken cancellationToken)
    {
        var (transactions, totalCount) = await _inventoryRepository.GetTransactionsByProductIdPagedAsync(
            request.ProductId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = transactions.Select(t => new InventoryTransactionDto(
            t.Id,
            t.ProductId,
            t.Type,
            t.Quantity,
            t.Reference,
            t.Notes,
            t.CreatedAt
        )).ToList();

        var result = new PagedResult<InventoryTransactionDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
