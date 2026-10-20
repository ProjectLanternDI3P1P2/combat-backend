using MediatR;

namespace Combat.Application.Abstractions;

public interface ICommand : ICommandMarker, IRequest;
