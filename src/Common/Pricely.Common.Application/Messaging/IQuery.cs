using MediatR;
using Pricely.Common.Domain;

namespace Pricely.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
