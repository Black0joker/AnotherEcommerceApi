using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    Guid? CategoryId,
    int InitialStock
) : ICommand<Guid>;
