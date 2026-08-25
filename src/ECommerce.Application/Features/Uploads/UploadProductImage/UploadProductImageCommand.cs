using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Uploads.UploadProductImage;

public record UploadProductImageCommand(
    Guid ProductId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Size,
    bool IsPrimary = false
) : ICommand<ProductImageDto>;
