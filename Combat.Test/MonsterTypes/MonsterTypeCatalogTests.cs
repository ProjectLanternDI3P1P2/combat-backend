using Combat.Application.Exceptions;
using Combat.Application.Features.MonsterTypes;
using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Infrastructure.MonsterMocks;
using FluentAssertions;
using FluentValidation;

namespace Combat.Test.MonsterTypes;

public class MonsterTypeCatalogTests
{
    private readonly MonsterTypeDefinitionValidator _validator = new();

    [Fact]
    public async Task GetAllAsync_MockSource_ReturnsStableUniqueDefinitions()
    {
        // Arrange
        var catalog = new MonsterTypeCatalog(new MockMonsterTypeSource(), _validator);

        // Act
        IReadOnlyList<MonsterTypeDefinition> first =
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<MonsterTypeDefinition> second =
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        first.Should().NotBeEmpty();
        first.Select(monsterType => monsterType.Id).Should().OnlyHaveUniqueItems();
        second.Select(monsterType => monsterType.Id).Should()
            .Equal(first.Select(monsterType => monsterType.Id));
    }

    [Fact]
    public async Task GetAllAsync_InvalidDefinition_RejectsWholeCatalogWithFieldErrors()
    {
        // Arrange
        var valid = CreateDefinition(Guid.NewGuid(), "Valid");
        var invalid = new MonsterTypeDefinition(Guid.Empty, "", false, 0, -1, -1, 0);
        var catalog = CreateCatalog([valid, invalid]);

        // Act
        Func<Task> act = async () =>
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        InvalidMonsterTypeCatalogException exception =
            (await act.Should().ThrowAsync<InvalidMonsterTypeCatalogException>()).Which;
        exception.Errors.Keys.Should().Contain([
            "monsterTypes[1].Id",
            "monsterTypes[1].Name",
            "monsterTypes[1].BaseHealth",
            "monsterTypes[1].BaseAttack",
            "monsterTypes[1].BaseDefense",
            "monsterTypes[1].BaseSpeed"
        ]);
    }

    [Fact]
    public async Task GetAllAsync_DuplicateSourceIds_RejectsWholeCatalogExplicitly()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        var catalog = CreateCatalog([
            CreateDefinition(duplicateId, "First"),
            CreateDefinition(duplicateId, "Second")
        ]);

        // Act
        Func<Task> act = async () =>
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        InvalidMonsterTypeCatalogException exception =
            (await act.Should().ThrowAsync<InvalidMonsterTypeCatalogException>()).Which;
        exception.Errors["monsterTypes"].Should().ContainSingle()
            .Which.Should().Contain(duplicateId.ToString());
    }

    [Fact]
    public async Task GetAllAsync_EmptySource_RejectsMissingCatalogExplicitly()
    {
        // Arrange
        var catalog = CreateCatalog([]);

        // Act
        Func<Task> act = async () =>
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        InvalidMonsterTypeCatalogException exception =
            (await act.Should().ThrowAsync<InvalidMonsterTypeCatalogException>()).Which;
        exception.Errors["monsterTypes"].Should().ContainSingle()
            .Which.Should().Contain("at least one monster type");
    }

    [Fact]
    public async Task GetAllAsync_NullSourceEntry_RejectsCatalogExplicitly()
    {
        // Arrange
        var catalog = CreateCatalog([null!]);

        // Act
        Func<Task> act = async () =>
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        InvalidMonsterTypeCatalogException exception =
            (await act.Should().ThrowAsync<InvalidMonsterTypeCatalogException>()).Which;
        exception.Errors["monsterTypes[0]"].Should().ContainSingle()
            .Which.Should().Be("Monster type definition is required.");
    }

    [Fact]
    public async Task ResolveRequiredAsync_KnownIds_ReturnsValidatedDefinitionsInRequestedOrder()
    {
        // Arrange
        MonsterTypeDefinition first = CreateDefinition(Guid.NewGuid(), "First");
        MonsterTypeDefinition second = CreateDefinition(Guid.NewGuid(), "Second");
        var catalog = CreateCatalog([first, second]);

        // Act
        IReadOnlyList<MonsterTypeDefinition> resolved = await catalog.ResolveRequiredAsync(
            [second.Id, first.Id],
            TestContext.Current.CancellationToken);

        // Assert
        resolved.Should().Equal(second, first);
    }

    [Fact]
    public async Task ResolveRequiredAsync_UnknownIdAmongKnownIds_RejectsSelectionAtomically()
    {
        // Arrange
        MonsterTypeDefinition first = CreateDefinition(Guid.NewGuid(), "First");
        MonsterTypeDefinition second = CreateDefinition(Guid.NewGuid(), "Second");
        Guid unknownId = Guid.NewGuid();
        var catalog = CreateCatalog([first, second]);

        // Act
        Func<Task> act = async () => await catalog.ResolveRequiredAsync(
            [first.Id, unknownId, second.Id],
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{unknownId}*");
    }

    [Theory]
    [MemberData(nameof(InvalidSelections))]
    public async Task ResolveRequiredAsync_InvalidIds_ThrowsValidationException(Guid[] monsterTypeIds)
    {
        // Arrange
        var catalog = CreateCatalog([CreateDefinition(Guid.NewGuid(), "Known")]);

        // Act
        Func<Task> act = async () => await catalog.ResolveRequiredAsync(
            monsterTypeIds,
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*MonsterTypeIds*");
    }

    [Fact]
    public async Task GetAllAsync_UnavailableSource_ThrowsDistinctSourceError()
    {
        // Arrange
        var catalog = new MonsterTypeCatalog(new UnavailableMonsterTypeSource(), _validator);

        // Act
        Func<Task> act = async () =>
            await catalog.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<MonsterTypeSourceUnavailableException>();
    }

    public static TheoryData<Guid[]> InvalidSelections
    {
        get
        {
            Guid duplicateId = Guid.NewGuid();
            return new TheoryData<Guid[]>
            {
                new[] { Guid.Empty },
                new[] { duplicateId, duplicateId }
            };
        }
    }

    private MonsterTypeCatalog CreateCatalog(IReadOnlyCollection<MonsterTypeDefinition> definitions)
    {
        return new MonsterTypeCatalog(new StubMonsterTypeSource(definitions), _validator);
    }

    private static MonsterTypeDefinition CreateDefinition(Guid id, string name)
    {
        return new MonsterTypeDefinition(id, name, false, 25, 8, 3, 6);
    }

    private sealed class StubMonsterTypeSource(IReadOnlyCollection<MonsterTypeDefinition> definitions)
        : IMonsterTypeSource
    {
        public Task<IReadOnlyCollection<MonsterTypeDefinition>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(definitions);
        }
    }
}
