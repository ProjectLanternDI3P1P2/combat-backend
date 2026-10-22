using Combat.Application.Models;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Combat.Application.Features.FightUseCase.StartFight;

public sealed class StartFightValidator : AbstractValidator<StartFightCommand>
{
    public StartFightValidator(
        IOptions<FightOptions> fightOptions,
        IOptions<MonsterGenerationOptions> generationOptions
    )
    {
        RuleFor(command => command.DungeonRunId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty();
        RuleFor(command => command.Floor).InclusiveBetween(1, generationOptions.Value.MaxFloor);
        RuleFor(command => command.Encounter).IsInEnum();
        RuleFor(command => command.HeroIds)
            .NotEmpty()
            .Must(ids => ids.Count <= fightOptions.Value.MaxHeroes)
            .WithMessage($"A party has at most {fightOptions.Value.MaxHeroes} heroes.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("A hero cannot fight twice in the same fight.");
        RuleForEach(command => command.HeroIds).NotEmpty();
        RuleFor(command => command.MonsterCount)
            .InclusiveBetween(1, fightOptions.Value.MaxMonsters)
            .When(command => command.MonsterCount is not null);
    }
}
