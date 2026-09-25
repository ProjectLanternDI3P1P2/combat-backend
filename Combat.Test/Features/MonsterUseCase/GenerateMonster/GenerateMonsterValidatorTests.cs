using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using FluentValidation.TestHelper;

namespace Combat.Test.Features.MonsterUseCase.GenerateMonster;

public class GenerateMonsterValidatorTests
{
    private readonly GenerateMonsterValidator _validator = new();

    private static GenerateMonsterCommand ValidCommand() => new(Guid.NewGuid(), Guid.NewGuid());

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
    public void Validate_EmptyMonsterTypeId_HasValidationErrorForMonsterTypeId()
    {
        // Arrange
        var command = ValidCommand() with { MonsterTypeId = Guid.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.MonsterTypeId);
    }
}
