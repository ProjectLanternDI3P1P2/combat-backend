using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using Combat.Application.Models;
using Combat.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;

namespace Combat.Test.Features.MonsterUseCase.GenerateMonster;

public sealed class GenerateMonsterValidatorTests
{
    private readonly GenerateMonsterValidator _validator = new(
        Options.Create(new MonsterGenerationOptions { MaxFloor = 40 })
    );

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        TestValidationResult<GenerateMonsterCommand> result = _validator.TestValidate(
            ValidCommand()
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(true, false, 1, 1, MonsterClass.Ordinary)]
    [InlineData(false, true, 1, 1, MonsterClass.Ordinary)]
    [InlineData(false, false, 0, 1, MonsterClass.Ordinary)]
    [InlineData(false, false, 41, 1, MonsterClass.Ordinary)]
    [InlineData(false, false, 1, 0, MonsterClass.Ordinary)]
    [InlineData(false, false, 1, 1, (MonsterClass)99)]
    public void Validate_InvalidCommand_HasValidationErrors(
        bool emptyDungeonRunId,
        bool emptyIdempotencyKey,
        int floor,
        int playerCount,
        MonsterClass monsterClass
    )
    {
        GenerateMonsterCommand command = new(
            emptyDungeonRunId ? Guid.Empty : Guid.NewGuid(),
            emptyIdempotencyKey ? Guid.Empty : Guid.NewGuid(),
            floor,
            playerCount,
            monsterClass
        );

        TestValidationResult<GenerateMonsterCommand> result = _validator.TestValidate(command);

        result.Errors.Should().NotBeEmpty();
    }

    private static GenerateMonsterCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 1, 1, MonsterClass.Ordinary);
}
