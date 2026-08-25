using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Uploads.DeleteProductImage;

public record DeleteProductImageCommand(Guid ImageId) : ICommand<bool>;
