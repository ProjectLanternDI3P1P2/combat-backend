using Combat.Application.Features.MonsterUseCase.GenerateMonster;
using Combat.Domain.Enums;
using Combat.Presentation.Controllers;
using Combat.Presentation.DTO;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ILogger = Serilog.ILogger;

namespace Combat.Test.Presentation;

public class MonsterControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly MonsterController _controller;

    public MonsterControllerTests()
    {
        _controller = new MonsterController(_mediator.Object, new Mock<ILogger>().Object);
    }

    [Fact]
    public async Task Generate_ValidRequest_ReturnsGeneratedMonster()
    {
        // Arrange
        var monsterDto = new MonsterDto { CombatId = Guid.NewGuid(), MonsterTypeId = Guid.NewGuid() };
        var result = new GenerateMonsterResult
        {
            MonsterId = Guid.NewGuid(),
            CombatId = monsterDto.CombatId,
            IsBoss = true,
            BaseHp = 300,
            BaseAttack = 40,
            BaseDefense = 25,
            BaseSpeed = 15,
            State = MonsterState.Alive
        };

        _mediator
            .Setup(mediator => mediator.Send(
                It.Is<GenerateMonsterCommand>(command =>
                    command.CombatId == monsterDto.CombatId && command.MonsterTypeId == monsterDto.MonsterTypeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        IActionResult action = await _controller.Generate(monsterDto, TestContext.Current.CancellationToken);

        // Assert
        OkObjectResult ok = action.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(result);
    }
}
