using Combat.Domain.Enums;

namespace Combat.Domain.Entities;

public class MonsterDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public MonsterClass Class { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int BaseHealth { get; set; }

    public int BaseAttack { get; set; }

    public int BaseDefense { get; set; }

    public int BaseSpeed { get; set; }
}
