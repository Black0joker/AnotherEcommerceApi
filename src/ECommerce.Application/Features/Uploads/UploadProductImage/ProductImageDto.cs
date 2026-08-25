namespace ECommerce.Application.Features.Uploads.UploadProductImage;

public record ProductImageDto(
    Guid Id,
    Guid ProductId,
    string FileName,
    string ContentType,
    long Size,
    string Url,
    int DisplayOrder,
    bool IsPrimary
);
