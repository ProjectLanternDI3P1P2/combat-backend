using Combat.Application.Abstractions;

namespace Combat.Application.Features.CombatUseCase.ReportCombatResult;

/// <summary>Records that Dungeon acknowledged the result of a finished combat.</summary>
public sealed record MarkCombatResultReportedCommand(Guid CombatId) : ICommand;
