namespace Combat.Application.Abstractions;

/// <summary>
/// Marks a request as a command. A command is a transaction boundary: the command
/// transaction behavior owns the commit of its changes.
/// </summary>
/// <remarks>
/// It is deliberately not <see cref="MediatR.IRequest"/>: a command that returns a result
/// implements <see cref="MediatR.IRequest{TResponse}"/>, and implementing both request
/// contracts at once makes the mediator overload resolution ambiguous.
/// </remarks>
public interface ICommandMarker;
