using Combat.Domain.Enums;

namespace Combat.Domain.ValueObjects;

/// <summary>A buff, debuff or status effect carried by a fighter during a combat.</summary>
public sealed record FighterEffect
{
    public FighterEffect(
        Guid effectId,
        string name,
        FighterEffectKind kind,
        int? remainingTurns = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (remainingTurns is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(remainingTurns),
                "Remaining turns cannot be negative."
            );
        }

        EffectId = effectId;
        Name = name;
        Kind = kind;
        RemainingTurns = remainingTurns;
    }

    public Guid EffectId { get; }
    public string Name { get; }
    public FighterEffectKind Kind { get; }

    /// <summary>Null for an effect with no intrinsic duration.</summary>
    public int? RemainingTurns { get; }
}
