using Combat.Application.Models;
using FluentValidation;

namespace Combat.Application.Features.MonsterTypes;

public sealed class MonsterTypeDefinitionValidator : AbstractValidator<MonsterTypeDefinition>
{
    public MonsterTypeDefinitionValidator()
    {
        RuleFor(monsterType => monsterType.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(monsterType => monsterType.Name)
            .NotEmpty().WithMessage("Name is required.");

        RuleFor(monsterType => monsterType.BaseHealth)
            .GreaterThan(0).WithMessage("BaseHealth must be greater than 0.");

        RuleFor(monsterType => monsterType.BaseAttack)
            .GreaterThan(0).WithMessage("BaseAttack must be greater than 0.");

        RuleFor(monsterType => monsterType.BaseDefense)
            .GreaterThanOrEqualTo(0).WithMessage("BaseDefense must be greater than or equal to 0.");

        RuleFor(monsterType => monsterType.BaseSpeed)
            .GreaterThan(0).WithMessage("BaseSpeed must be greater than 0.");
    }
}
