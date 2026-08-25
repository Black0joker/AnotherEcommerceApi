using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Inventory.GetInventory;

public class GetInventoryQueryHandler : IQueryHandler<GetInventoryQuery, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<InventoryDto>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (inventory is null)
        {
            return Result.Failure<InventoryDto>(Error.NotFound(
                "Inventory.NotFound",
                $"No inventory record found for product '{request.ProductId}'."));
        }

        var dto = new InventoryDto(
            inventory.Id,
            inventory.ProductId,
            inventory.AvailableQuantity,
            inventory.ReservedQuantity,
            inventory.TotalQuantity,
            inventory.UpdatedAt);

        return Result.Success(dto);
    }
}
