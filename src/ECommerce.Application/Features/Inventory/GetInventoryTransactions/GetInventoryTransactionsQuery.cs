using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Inventory.GetInventoryTransactions;

public record GetInventoryTransactionsQuery(Guid ProductId) : IQuery<IReadOnlyList<InventoryTransactionDto>>;
