using Combat.Application.Features.CombatUseCase.CreateCombat;
using Combat.Domain.Enums;
using FluentAssertions;

namespace Combat.Test.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatValidatorTests
{
    private readonly CreateCombatValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();

        var result = _validator.Validate(
            new CreateCombatCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Enemy
            )
        );

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithoutSessionId_Fails()
    {
        var result = _validator.Validate(
            new CreateCombatCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), EncounterType.Enemy)
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "SessionId");
    }

    [Fact]
    public void Validate_WithoutEncounterId_Fails()
    {
        var result = _validator.Validate(
            new CreateCombatCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), EncounterType.Enemy)
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "EncounterId");
    }

    [Fact]
    public void Validate_WithoutCommandId_Fails()
    {
        var result = _validator.Validate(
            new CreateCombatCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, EncounterType.Enemy)
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "CommandId");
    }

    [Fact]
    public void Validate_UnknownEncounterType_Fails()
    {
        var result = _validator.Validate(
            new CreateCombatCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                (EncounterType)42
            )
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "EncounterType");
    }
}
