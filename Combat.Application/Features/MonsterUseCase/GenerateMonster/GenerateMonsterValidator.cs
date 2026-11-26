using Combat.Application.Models;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public sealed class GenerateMonsterValidator : AbstractValidator<GenerateMonsterCommand>
{
    public GenerateMonsterValidator(IOptions<MonsterGenerationOptions> options)
    {
        MonsterGenerationOptions generationOptions = options.Value;

        RuleFor(command => command.DungeonRunId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty();
        RuleFor(command => command.Floor).InclusiveBetween(1, generationOptions.MaxFloor);
        RuleFor(command => command.PlayerCount).GreaterThanOrEqualTo(1);
        RuleFor(command => command.MonsterClass).IsInEnum();
    }
}
