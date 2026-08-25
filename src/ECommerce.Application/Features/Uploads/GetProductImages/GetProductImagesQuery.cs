using ECommerce.Application.Common;
using ECommerce.Application.Features.Uploads.UploadProductImage;

namespace ECommerce.Application.Features.Uploads.GetProductImages;

public record GetProductImagesQuery(Guid ProductId) : IQuery<IReadOnlyList<ProductImageDto>>;
