using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Uploads.UploadProductImage;

public class UploadProductImageCommandHandler : ICommandHandler<UploadProductImageCommand, ProductImageDto>
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public UploadProductImageCommandHandler(
        IProductImageRepository imageRepository,
        IProductRepository productRepository,
        IFileStorage fileStorage,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _imageRepository = imageRepository;
        _productRepository = productRepository;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductImageDto>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        // Validate authentication
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<ProductImageDto>(Error.Unauthorized(
                "Upload.Unauthorized",
                "You must be authenticated to upload images."));
        }

        // Validate product exists
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductImageDto>(Error.NotFound(
                "Product.NotFound",
                $"Product with ID '{request.ProductId}' was not found."));
        }

        // Validate file size
        if (request.Size > MaxFileSize)
        {
            return Result.Failure<ProductImageDto>(Error.Validation(
                "Upload.FileTooLarge",
                $"File size exceeds the maximum allowed size of {MaxFileSize / 1024 / 1024} MB."));
        }

        // Validate content type
        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result.Failure<ProductImageDto>(Error.Validation(
                "Upload.InvalidContentType",
                $"Content type '{request.ContentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}"));
        }

        // Validate file extension
        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Failure<ProductImageDto>(Error.Validation(
                "Upload.InvalidExtension",
                $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}"));
        }

        // Validate filename (prevent path traversal)
        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Contains("..") || safeFileName.Contains("/") || safeFileName.Contains("\\"))
        {
            return Result.Failure<ProductImageDto>(Error.Validation(
                "Upload.InvalidFileName",
                "The filename contains invalid characters."));
        }

        // Save file to storage
        var storageKey = await _fileStorage.SaveAsync(request.FileStream, safeFileName, request.ContentType, cancellationToken);

        // Get display order
        var existingImages = await _imageRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        var nextDisplayOrder = existingImages.Count > 0
            ? existingImages.Max(i => i.DisplayOrder) + 1
            : 0;

        // If this is primary, unset other primary images
        if (request.IsPrimary)
        {
            foreach (var img in existingImages.Where(i => i.IsPrimary))
            {
                img.IsPrimary = false;
                _imageRepository.Update(img);
            }
        }

        var productImage = new ProductImage
        {
            ProductId = request.ProductId,
            StorageKey = storageKey,
            FileName = safeFileName,
            ContentType = request.ContentType,
            Size = request.Size,
            Url = _fileStorage.GetUrl(storageKey),
            DisplayOrder = nextDisplayOrder,
            IsPrimary = request.IsPrimary || existingImages.Count == 0
        };

        await _imageRepository.AddAsync(productImage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ProductImageDto(
            productImage.Id,
            productImage.ProductId,
            productImage.FileName,
            productImage.ContentType,
            productImage.Size,
            productImage.Url,
            productImage.DisplayOrder,
            productImage.IsPrimary);

        return Result.Success(dto);
    }
}
