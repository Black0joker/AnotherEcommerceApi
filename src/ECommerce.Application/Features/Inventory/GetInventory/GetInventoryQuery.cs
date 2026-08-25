using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Inventory.GetInventory;

public record GetInventoryQuery(Guid ProductId) : IQuery<InventoryDto>;
