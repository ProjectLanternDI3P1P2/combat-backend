using Combat.Domain.Enums;
using FluentValidation;

namespace Combat.Application.Features.FightUseCase.SubmitFightAction;

public sealed class SubmitFightActionValidator : AbstractValidator<SubmitFightActionCommand>
{
    public SubmitFightActionValidator()
    {
        RuleFor(command => command.FightId).NotEmpty();
        RuleFor(command => command.CommandId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.TargetId)
            .NotEmpty()
            .When(command => command.Type == FightActionType.Attack);
        RuleFor(command => command.AbilityId)
            .NotEmpty()
            .When(command => command.Type == FightActionType.Ability);
        RuleFor(command => command.ItemId)
            .NotEmpty()
            .When(command => command.Type == FightActionType.Item);
    }
}
