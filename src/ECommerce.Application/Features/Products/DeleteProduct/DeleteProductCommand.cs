using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.DeleteProduct;

public record DeleteProductCommand(Guid Id) : ICommand;
