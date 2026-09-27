using FluentValidation;

namespace Combat.Application.Features.MonsterTypes.GetMonsterTypeById;

public sealed class GetMonsterTypeByIdQueryValidator : AbstractValidator<GetMonsterTypeByIdQuery>
{
    public GetMonsterTypeByIdQueryValidator()
    {
        RuleFor(query => query.MonsterTypeId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("MonsterTypeId is required.")
            .Must(monsterTypeId => Guid.TryParse(monsterTypeId, out Guid parsed) && parsed != Guid.Empty)
            .WithMessage("MonsterTypeId must be a non-empty GUID.");
    }
}
