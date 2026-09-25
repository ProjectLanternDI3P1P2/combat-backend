using FluentValidation;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public class GenerateMonsterValidator : AbstractValidator<GenerateMonsterCommand>
{
    public GenerateMonsterValidator()
    {
        RuleFor(m => m.CombatId)
            .NotEmpty().WithMessage("CombatId is required.");

        RuleFor(m => m.MonsterTypeId)
            .NotEmpty().WithMessage("MonsterTypeId is required.");
    }
}
