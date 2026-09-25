using Combat.Application.Models.HeroCombat;

namespace Combat.Infrastructure.ExternalServices.Player;

/// <summary>
/// Heroes served while the Player service contract is not available.
/// Fixed identifiers make manual tests reproducible.
/// </summary>
public static class MockHeroCatalog
{
    public static readonly Guid WarriorHeroId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid MageHeroId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid WoundedRogueHeroId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Dictionary<Guid, HeroCombatSnapshot> Heroes = new[]
    {
        new HeroCombatSnapshot
        {
            HeroId = WarriorHeroId,
            Name = "Aldric",
            Level = 3,
            CurrentHp = 120,
            MaxHp = 120,
            CurrentMana = 20,
            MaxMana = 20,
            Stats = new HeroCombatStats { Attack = 18, Defense = 12, Speed = 6 },
            Abilities =
            [
                Ability("a1000000-0000-0000-0000-000000000001", "Heavy Strike", 5, "SingleEnemy"),
                Ability("a1000000-0000-0000-0000-000000000002", "Shield Wall", 8, "Self")
            ],
            IsMocked = true
        },
        new HeroCombatSnapshot
        {
            HeroId = MageHeroId,
            Name = "Lyra",
            Level = 3,
            CurrentHp = 70,
            MaxHp = 70,
            CurrentMana = 80,
            MaxMana = 80,
            Stats = new HeroCombatStats { Attack = 8, Defense = 5, Speed = 9 },
            Abilities =
            [
                Ability("a2000000-0000-0000-0000-000000000001", "Fireball", 15, "SingleEnemy"),
                Ability("a2000000-0000-0000-0000-000000000002", "Frost Nova", 25, "AllEnemies"),
                Ability("a2000000-0000-0000-0000-000000000003", "Heal", 12, "SingleAlly")
            ],
            IsMocked = true
        },
        new HeroCombatSnapshot
        {
            // Hero carried into a combat with the HP/mana left from previous rooms.
            HeroId = WoundedRogueHeroId,
            Name = "Kael",
            Level = 2,
            CurrentHp = 25,
            MaxHp = 85,
            CurrentMana = 10,
            MaxMana = 40,
            Stats = new HeroCombatStats { Attack = 14, Defense = 7, Speed = 14 },
            Abilities =
            [
                Ability("a3000000-0000-0000-0000-000000000001", "Backstab", 10, "SingleEnemy")
            ],
            IsMocked = true
        }
    }.ToDictionary(hero => hero.HeroId);

    public static HeroCombatSnapshot? Find(Guid heroId) => Heroes.GetValueOrDefault(heroId);

    private static HeroAbility Ability(string abilityId, string name, int manaCost, string targetType) => new()
    {
        AbilityId = Guid.Parse(abilityId),
        Name = name,
        ManaCost = manaCost,
        TargetType = targetType
    };
}
