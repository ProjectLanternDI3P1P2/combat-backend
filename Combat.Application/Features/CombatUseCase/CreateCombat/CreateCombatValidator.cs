using FluentValidation;

namespace Combat.Application.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatValidator : AbstractValidator<CreateCombatCommand>
{
    public CreateCombatValidator()
    {
        RuleFor(command => command.SessionId).NotEmpty().WithMessage("SessionId is required.");
        RuleFor(command => command.EncounterId).NotEmpty().WithMessage("EncounterId is required.");
        RuleFor(command => command.CommandId).NotEmpty().WithMessage("CommandId is required.");
        RuleFor(command => command.EncounterType)
            .IsInEnum()
            .WithMessage("EncounterType must be a known encounter type.");
    }
}
