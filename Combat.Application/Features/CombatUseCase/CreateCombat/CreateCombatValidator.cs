using Combat.Application.Models;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatValidator : AbstractValidator<CreateCombatCommand>
{
    public CreateCombatValidator(
        IOptions<MonsterGenerationOptions> monsterOptions,
        IOptions<CombatOptions> combatOptions
    )
    {
        RuleFor(command => command.SessionId).NotEmpty().WithMessage("SessionId is required.");
        RuleFor(command => command.EncounterId).NotEmpty().WithMessage("EncounterId is required.");
        RuleFor(command => command.CommandId).NotEmpty().WithMessage("CommandId is required.");
        RuleFor(command => command.RunId).NotEmpty().WithMessage("RunId is required.");
        RuleFor(command => command.EncounterType)
            .IsInEnum()
            .WithMessage("EncounterType must be a known encounter type.");
        RuleFor(command => command.Floor)
            .InclusiveBetween(1, monsterOptions.Value.MaxFloor)
            .WithMessage("Floor must be between 1 and the last floor (1-based).");
        RuleFor(command => command.Participants)
            .NotEmpty()
            .WithMessage("At least one hero is required.")
            .Must(participants => participants.Count <= combatOptions.Value.MaxHeroes)
            .WithMessage("Too many heroes for one combat.")
            .Must(participants =>
                participants.Select(participant => participant.HeroId).Distinct().Count()
                == participants.Count
            )
            .WithMessage("Each hero can join the combat once.");
        RuleForEach(command => command.Participants)
            .Must(participant => participant.HeroId != Guid.Empty)
            .WithMessage("HeroId is required.");
        RuleFor(command => command.Monsters)
            .NotEmpty()
            .WithMessage("At least one monster is required.")
            .Must(monsters => monsters.Count <= monsterOptions.Value.MaxMonstersPerEncounter)
            .WithMessage("Too many monsters for one encounter.")
            .Must(monsters =>
                monsters.Select(monster => monster.ElementId).Distinct().Count() == monsters.Count
            )
            .WithMessage("Each mob can join the combat once.");
        RuleForEach(command => command.Monsters)
            .Must(monster => monster.ElementId != Guid.Empty)
            .WithMessage("ElementId is required.");
    }
}
