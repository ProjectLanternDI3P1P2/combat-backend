using Combat.Application.Abstractions;
using Combat.Domain.Enums;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

/// <summary>
/// Creates a combat instance from an encounter reported by the Dungeon service.
/// The same command never creates two combats.
/// </summary>
public sealed record CreateCombatCommand(
    Guid SessionId,
    Guid EncounterId,
    Guid CommandId,
    EncounterType EncounterType
) : ICommand<CreateCombatResult>;
