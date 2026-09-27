using FluentValidation;

namespace Combat.Application.Features.HeroUseCase.GetHeroCombatData;

public class GetHeroCombatDataValidator : AbstractValidator<GetHeroCombatDataQuery>
{
    public GetHeroCombatDataValidator()
    {
        RuleFor(q => q.HeroId)
            .NotEmpty().WithMessage("HeroId is required.");
    }
}
