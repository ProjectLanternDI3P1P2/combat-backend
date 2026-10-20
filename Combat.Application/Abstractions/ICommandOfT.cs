using MediatR;

namespace Combat.Application.Abstractions;

public interface ICommand<out TResponse> : ICommandMarker, IRequest<TResponse>;
