using MediatR;

namespace ECommerce.Application.Common;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
