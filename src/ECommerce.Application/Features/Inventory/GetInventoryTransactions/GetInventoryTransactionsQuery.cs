using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Inventory.GetInventoryTransactions;

public class GetInventoryTransactionsQuery : PagedQuery, IQuery<PagedResult<InventoryTransactionDto>>
{
    public Guid ProductId { get; set; }
}
