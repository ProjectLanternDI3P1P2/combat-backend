using FluentValidation;

namespace Combat.Application.Features.CombatUseCase.JoinCombat;

public sealed class JoinCombatValidator : AbstractValidator<JoinCombatCommand>
{
    public JoinCombatValidator()
    {
        RuleFor(command => command.CombatId).NotEmpty().WithMessage("CombatId is required.");
    }
}
