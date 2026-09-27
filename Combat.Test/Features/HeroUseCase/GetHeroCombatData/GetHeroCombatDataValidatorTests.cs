using Combat.Application.Features.HeroUseCase.GetHeroCombatData;
using FluentAssertions;

namespace Combat.Test.Features.HeroUseCase.GetHeroCombatData;

public class GetHeroCombatDataValidatorTests
{
    private readonly GetHeroCombatDataValidator _validator = new();

    [Fact]
    public void Validate_EmptyHeroId_HasError()
    {
        // Act
        var result = _validator.Validate(new GetHeroCombatDataQuery(Guid.Empty));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetHeroCombatDataQuery.HeroId));
    }

    [Fact]
    public void Validate_HeroIdProvided_IsValid()
    {
        // Act
        var result = _validator.Validate(new GetHeroCombatDataQuery(Guid.NewGuid()));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
