using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Features.Uploads.UploadProductImage;

namespace ECommerce.Application.Features.Uploads.GetProductImages;

public class GetProductImagesQueryHandler : IQueryHandler<GetProductImagesQuery, IReadOnlyList<ProductImageDto>>
{
    private readonly IProductImageRepository _imageRepository;

    public GetProductImagesQueryHandler(IProductImageRepository imageRepository)
    {
        _imageRepository = imageRepository;
    }

    public async Task<Result<IReadOnlyList<ProductImageDto>>> Handle(GetProductImagesQuery request, CancellationToken cancellationToken)
    {
        var images = await _imageRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        var dtos = images.Select(i => new ProductImageDto(
            i.Id,
            i.ProductId,
            i.FileName,
            i.ContentType,
            i.Size,
            i.Url,
            i.DisplayOrder,
            i.IsPrimary
        )).ToList();

        return Result.Success<IReadOnlyList<ProductImageDto>>(dtos);
    }
}
