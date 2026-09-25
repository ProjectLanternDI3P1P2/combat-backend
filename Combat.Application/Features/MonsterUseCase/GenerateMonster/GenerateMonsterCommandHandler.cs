using Combat.Application.Models;
using Combat.Application.Ports;
using Combat.Domain.Entities;
using Combat.Domain.Enums;
using Combat.Domain.Repositories;
using MediatR;

namespace Combat.Application.Features.MonsterUseCase.GenerateMonster;

public sealed class GenerateMonsterCommandHandler(
    IMonsterTypeProvider monsterTypeProvider,
    IMonsterRepository monsterRepository) : IRequestHandler<GenerateMonsterCommand, GenerateMonsterResult>
{
    public async Task<GenerateMonsterResult> Handle(GenerateMonsterCommand request, CancellationToken cancellationToken)
    {
        MonsterTypeSummary? monsterType = await monsterTypeProvider.GetMonsterTypeAsync(request.MonsterType, cancellationToken);

        if (monsterType is null)
        {
            throw new KeyNotFoundException($"Monster type not found with MonsterType '{request.MonsterType}'.");
        }

        Monster monster = new()
        {
            MonsterId = Guid.NewGuid(),
            CombatId = request.CombatId,
            IsBoss = monsterType.IsBoss,
            BaseHp = monsterType.BaseHp,
            BaseAttack = monsterType.BaseAttack,
            BaseDefense = monsterType.BaseDefense,
            BaseSpeed = monsterType.BaseSpeed,
            State = MonsterState.Alive
        };

        await monsterRepository.AddMonsterAsync(monster, cancellationToken);

        return new GenerateMonsterResult
        {
            MonsterId = monster.MonsterId,
            CombatId = monster.CombatId,
            IsBoss = monster.IsBoss,
            BaseHp = monster.BaseHp,
            BaseAttack = monster.BaseAttack,
            BaseDefense = monster.BaseDefense,
            BaseSpeed = monster.BaseSpeed,
            State = monster.State
        };
    }
}
