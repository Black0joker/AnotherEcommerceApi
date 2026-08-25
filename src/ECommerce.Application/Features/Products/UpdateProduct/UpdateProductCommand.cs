using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    Guid? CategoryId,
    bool IsActive
) : ICommand;
