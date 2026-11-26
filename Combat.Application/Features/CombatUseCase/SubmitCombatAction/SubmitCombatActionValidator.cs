using Combat.Domain.Enums;
using FluentValidation;

namespace Combat.Application.Features.CombatUseCase.SubmitCombatAction;

public sealed class SubmitCombatActionValidator : AbstractValidator<SubmitCombatActionCommand>
{
    public SubmitCombatActionValidator()
    {
        RuleFor(command => command.CombatId).NotEmpty().WithMessage("CombatId is required.");
        RuleFor(command => command.CommandId).NotEmpty().WithMessage("CommandId is required.");
        RuleFor(command => command.ActorId).NotEmpty().WithMessage("ActorId is required.");
        RuleFor(command => command.Type).IsInEnum().WithMessage("Type must be a known action.");
        RuleFor(command => command.AbilityCode)
            .NotEmpty()
            .When(command => command.Type == FightActionType.Ability)
            .WithMessage("AbilityCode is required for an ability.");
        RuleFor(command => command.TargetId)
            .NotEmpty()
            .When(command => command.Type == FightActionType.Attack)
            .WithMessage("TargetId is required for an attack.");
    }
}
