using Combat.Application.Abstractions;

namespace Combat.Application.Features.PlayerUseCase.CreatePlayer;

public record CreatePlayerCommand(
    Guid Id,
    string Name,
    int Attack,
    int Health,
    int MaxHealth) : ICommand;
