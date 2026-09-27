using Combat.Application.Features.MonsterTypes.GetMonsterTypeById;
using Combat.Application.Features.MonsterTypes.GetMonsterTypes;
using Combat.Application.Models;
using Combat.Presentation.Controllers;
using Combat.Presentation.DTO;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.Presentation;

public class MonsterTypesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly MonsterTypesController _controller;

    public MonsterTypesControllerTests()
    {
        _controller = new MonsterTypesController(_mediator.Object, new Mock<ILogger>().Object);
    }

    [Fact]
    public async Task GetAll_ValidatedCatalog_ReturnsMappedDtos()
    {
        // Arrange
        var monsterType = new MonsterTypeDefinition(Guid.NewGuid(), "Cave Rat", false, 18, 5, 1, 12);
        _mediator
            .Setup(mediator => mediator.Send(
                It.IsAny<GetMonsterTypesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { monsterType });

        // Act
        ActionResult<IReadOnlyList<MonsterTypeDto>> action =
            await _controller.GetAll(TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        MonsterTypeDto[] response = ok.Value.Should().BeOfType<MonsterTypeDto[]>().Subject;
        response.Should().ContainSingle().Which.Should().Be(MonsterTypeDto.From(monsterType));
    }

    [Fact]
    public async Task GetById_IdText_ForwardsValidationToApplicationQuery()
    {
        // Arrange
        Guid monsterTypeId = Guid.NewGuid();
        var monsterType = new MonsterTypeDefinition(monsterTypeId, "Cave Rat", false, 18, 5, 1, 12);
        _mediator
            .Setup(mediator => mediator.Send(
                It.Is<GetMonsterTypeByIdQuery>(query => query.MonsterTypeId == monsterTypeId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(monsterType);

        // Act
        ActionResult<MonsterTypeDto> action = await _controller.GetById(
            monsterTypeId.ToString(),
            TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(MonsterTypeDto.From(monsterType));
    }
}
