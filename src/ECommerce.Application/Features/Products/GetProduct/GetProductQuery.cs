using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetProduct;

public record GetProductQuery(Guid Id) : IQuery<ProductDto>;
