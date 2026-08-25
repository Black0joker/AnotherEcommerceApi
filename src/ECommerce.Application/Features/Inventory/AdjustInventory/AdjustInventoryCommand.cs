using ECommerce.Application.Common;
using ECommerce.Application.Features.Inventory.GetInventory;

namespace ECommerce.Application.Features.Inventory.AdjustInventory;

public record AdjustInventoryCommand(
    Guid ProductId,
    int Quantity,
    string? Reason
) : ICommand<InventoryDto>;
