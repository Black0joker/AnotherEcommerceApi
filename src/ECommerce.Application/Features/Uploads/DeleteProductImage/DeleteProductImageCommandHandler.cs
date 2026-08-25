using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Uploads.DeleteProductImage;

public class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, bool>
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductImageCommandHandler(
        IProductImageRepository imageRepository,
        IFileStorage fileStorage,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _imageRepository = imageRepository;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<bool>(Error.Unauthorized(
                "Upload.Unauthorized",
                "You must be authenticated to delete images."));
        }

        var image = await _imageRepository.GetByIdAsync(request.ImageId, cancellationToken);

        if (image is null)
        {
            return Result.Failure<bool>(Error.NotFound(
                "Image.NotFound",
                $"Image with ID '{request.ImageId}' was not found."));
        }

        // Delete file from storage
        await _fileStorage.DeleteAsync(image.StorageKey, cancellationToken);

        // Remove from database
        _imageRepository.Delete(image);

        // If deleted image was primary, promote the first remaining image
        if (image.IsPrimary)
        {
            var remainingImages = await _imageRepository.GetByProductIdAsync(image.ProductId, cancellationToken);
            var nextPrimary = remainingImages.FirstOrDefault();
            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;
                _imageRepository.Update(nextPrimary);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
