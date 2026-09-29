using Combat.Domain.Enums;

namespace Combat.Domain.Entities;

public class Monster
{
    public Guid MonsterId { get; set; }
    public Guid CombatId { get; set; }
    public bool IsBoss { get; set; }
    public int BaseHp { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpeed { get; set; }
    public MonsterState State { get; set; }
}
