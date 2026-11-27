namespace Combat.Application.Models.HeroCombat;

/// <summary>Cosmetic codes chosen in the Player hero editor; never an image.</summary>
/// <param name="HairColor">Null when the hero is bald.</param>
public sealed record HeroAppearanceSnapshot(int SkinTone, string HairStyle, int? HairColor);
