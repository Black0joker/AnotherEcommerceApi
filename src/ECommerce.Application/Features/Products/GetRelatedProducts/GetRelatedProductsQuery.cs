using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Products.GetRelatedProducts;

public record GetRelatedProductsQuery(Guid ProductId, int Count = 8) : IQuery<IReadOnlyList<RelatedProductDto>>;
