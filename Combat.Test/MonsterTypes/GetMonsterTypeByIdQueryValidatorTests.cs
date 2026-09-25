using Combat.Application.Features.MonsterTypes.GetMonsterTypeById;
using FluentValidation.TestHelper;

namespace Combat.Test.MonsterTypes;

public class GetMonsterTypeByIdQueryValidatorTests
{
    private readonly GetMonsterTypeByIdQueryValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Validate_InvalidId_HasValidationErrorForMonsterTypeId(string monsterTypeId)
    {
        // Act
        TestValidationResult<GetMonsterTypeByIdQuery> result =
            _validator.TestValidate(new GetMonsterTypeByIdQuery(monsterTypeId));

        // Assert
        result.ShouldHaveValidationErrorFor(query => query.MonsterTypeId);
    }

    [Fact]
    public void Validate_NonEmptyGuid_HasNoValidationErrors()
    {
        // Act
        TestValidationResult<GetMonsterTypeByIdQuery> result =
            _validator.TestValidate(new GetMonsterTypeByIdQuery(Guid.NewGuid().ToString()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
