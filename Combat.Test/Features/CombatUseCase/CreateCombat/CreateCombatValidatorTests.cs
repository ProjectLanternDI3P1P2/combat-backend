using Combat.Application.Features.CombatUseCase.CreateCombat;
using Combat.Application.Models;
using Combat.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Combat.Test.Features.CombatUseCase.CreateCombat;

public sealed class CreateCombatValidatorTests
{
    private readonly CreateCombatValidator _validator = new(
        Options.Create(new MonsterGenerationOptions { MaxMonstersPerEncounter = 6, MaxFloor = 40 }),
        Options.Create(new CombatOptions { MaxHeroes = 4 })
    );

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("SessionId")]
    [InlineData("EncounterId")]
    [InlineData("CommandId")]
    [InlineData("RunId")]
    public void Validate_WithoutIdentifier_Fails(string property)
    {
        CreateCombatCommand command = property switch
        {
            "SessionId" => Valid() with { SessionId = Guid.Empty },
            "EncounterId" => Valid() with { EncounterId = Guid.Empty },
            "CommandId" => Valid() with { CommandId = Guid.Empty },
            _ => Valid() with { RunId = Guid.Empty },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == property);
    }

    [Fact]
    public void Validate_UnknownEncounterType_Fails()
    {
        _validator
            .Validate(Valid() with { EncounterType = (EncounterType)42 })
            .Errors.Should()
            .Contain(error => error.PropertyName == "EncounterType");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(41)]
    public void Validate_FloorOutOfRange_Fails(int floor)
    {
        _validator.Validate(Valid() with { Floor = floor }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoMonster_Fails()
    {
        _validator.Validate(Valid() with { Monsters = [] }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MoreMonstersThanTheMaximum_Fails()
    {
        CreateCombatCommand command = Valid() with
        {
            Monsters =
            [
                .. Enumerable.Range(0, 7).Select(_ => new EncounterMonster(Guid.NewGuid(), false)),
            ],
        };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SameMobTwice_Fails()
    {
        Guid elementId = Guid.NewGuid();
        CreateCombatCommand command = Valid() with
        {
            Monsters =
            [
                new EncounterMonster(elementId, false),
                new EncounterMonster(elementId, false),
            ],
        };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoHero_Fails()
    {
        _validator.Validate(Valid() with { Participants = [] }).IsValid.Should().BeFalse();
    }

    private static CreateCombatCommand Valid() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EncounterType.Enemy,
            Guid.NewGuid(),
            1,
            0,
            [new EncounterParticipant(Guid.NewGuid(), null, null)],
            [new EncounterMonster(Guid.NewGuid(), false)]
        );
}
