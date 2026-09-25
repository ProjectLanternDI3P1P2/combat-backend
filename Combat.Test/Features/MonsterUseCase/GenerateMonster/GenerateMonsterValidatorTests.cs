using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using FluentValidation.TestHelper;

namespace Combat.Test.Features.MonsterUseCase.GenerateMonster;

public class GenerateMonsterValidatorTests
{
    private readonly GenerateMonsterValidator _validator = new();

    private static GenerateMonsterCommand ValidCommand() => new(Guid.NewGuid(), "goblin");

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = ValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCombatId_HasValidationErrorForCombatId()
    {
        // Arrange
        var command = ValidCommand() with { CombatId = Guid.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.CombatId);
    }

    [Fact]
    public void Validate_EmptyMonsterType_HasValidationErrorForMonsterType()
    {
        // Arrange
        var command = ValidCommand() with { MonsterType = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.MonsterType);
    }

    [Fact]
    public void Validate_MonsterTypeExceedsMaximumLength_HasValidationErrorForMonsterType()
    {
        // Arrange
        var command = ValidCommand() with { MonsterType = new string('a', 51) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.MonsterType);
    }
}
