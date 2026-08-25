using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Inventory.GetInventory;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Inventory.AdjustInventory;

public class AdjustInventoryCommandHandler : ICommandHandler<AdjustInventoryCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustInventoryCommandHandler(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InventoryDto>> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity == 0)
        {
            return Result.Failure<InventoryDto>(Error.Validation(
                "Inventory.InvalidAdjustment",
                "Adjustment quantity cannot be zero."));
        }

        var inventory = await _inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (inventory is null)
        {
            return Result.Failure<InventoryDto>(Error.NotFound(
                "Inventory.NotFound",
                $"No inventory record found for product '{request.ProductId}'."));
        }

        // Validate adjustment won't result in negative stock
        if (inventory.AvailableQuantity + request.Quantity < 0)
        {
            return Result.Failure<InventoryDto>(Error.Validation(
                "Inventory.InsufficientStock",
                $"Cannot reduce stock by {Math.Abs(request.Quantity)}. Only {inventory.AvailableQuantity} available."));
        }

        // Apply adjustment
        inventory.Adjust(request.Quantity);

        // Create audit trail transaction
        var transaction = new InventoryTransaction
        {
            InventoryItemId = inventory.Id,
            ProductId = request.ProductId,
            Type = InventoryTransactionType.Adjustment,
            Quantity = request.Quantity,
            Reference = "Manual Adjustment",
            Notes = request.Reason
        };

        await _inventoryRepository.AddTransactionAsync(transaction, cancellationToken);
        _inventoryRepository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
