namespace Combat.Domain.ValueObjects;

/// <summary>
/// What a client needs to draw a fighter, as codes only, never an image (Player rule:
/// squads exchange codes). A hero carries the Player class and appearance codes; a
/// monster carries the sprite key of its definition.
/// </summary>
public sealed record FighterVisual(
    string Kind,
    string? ClassCode = null,
    int? SkinTone = null,
    string? HairStyle = null,
    int? HairColor = null,
    string? SpriteKey = null,
    bool IsBoss = false
)
{
    public const string HeroKind = "hero";
    public const string MonsterKind = "monster";

    public bool HasAppearance => SkinTone is not null && HairStyle is not null;

    public static FighterVisual Hero(
        string classCode,
        int? skinTone,
        string? hairStyle,
        int? hairColor
    ) => new(HeroKind, classCode, skinTone, hairStyle, hairColor);

    public static FighterVisual Monster(string spriteKey, bool isBoss) =>
        new(MonsterKind, SpriteKey: spriteKey, IsBoss: isBoss);
}
