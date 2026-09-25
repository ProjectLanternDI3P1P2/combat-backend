using FluentValidation;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public class GenerateMonsterValidator : AbstractValidator<GenerateMonsterCommand>
{
    public GenerateMonsterValidator()
    {
        RuleFor(m => m.CombatId)
            .NotEmpty().WithMessage("CombatId is required.");

        RuleFor(m => m.MonsterType)
            .NotEmpty().WithMessage("MonsterType is required.")
            .MaximumLength(50).WithMessage("MonsterType cannot exceed 50 characters.");
    }
}
