using Combat.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Combat.Infrastructure.Persistence.Configurations;

public sealed class CombatFighterConfiguration : IEntityTypeConfiguration<CombatFighterRecord>
{
    public void Configure(EntityTypeBuilder<CombatFighterRecord> builder)
    {
        builder.ToTable("CombatFighters");

        builder.HasKey(fighter => fighter.FighterId);
        builder.Property(fighter => fighter.FighterId).HasColumnName("CombatFighterId");
        builder.Property(fighter => fighter.CombatId).HasColumnName("CombatId");
        builder
            .Property(fighter => fighter.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("CombatFighterType");
        builder.Property(fighter => fighter.ExternalId).HasColumnName("CombatFighterExternalId");
        builder
            .Property(fighter => fighter.Name)
            .HasMaxLength(100)
            .HasColumnName("CombatFighterName");
        builder.Property(fighter => fighter.Level).HasColumnName("CombatFighterLevel");
        builder
            .Property(fighter => fighter.IsFromMockedData)
            .HasColumnName("CombatFighterIsFromMockedData");
        builder.Property(fighter => fighter.CurrentHp).HasColumnName("CombatFighterCurrentHp");
        builder.Property(fighter => fighter.MaxHp).HasColumnName("CombatFighterMaxHp");
        builder.Property(fighter => fighter.CurrentMana).HasColumnName("CombatFighterCurrentMana");
        builder.Property(fighter => fighter.MaxMana).HasColumnName("CombatFighterMaxMana");
        builder.Property(fighter => fighter.Attack).HasColumnName("CombatFighterAttack");
        builder.Property(fighter => fighter.Defense).HasColumnName("CombatFighterDefense");
        builder.Property(fighter => fighter.Speed).HasColumnName("CombatFighterSpeed");
        builder
            .Property(fighter => fighter.State)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("CombatFighterState");
        builder
            .Property(fighter => fighter.InitialCurrentHp)
            .HasColumnName("CombatFighterInitialCurrentHp");
        builder
            .Property(fighter => fighter.InitialMaxHp)
            .HasColumnName("CombatFighterInitialMaxHp");
        builder
            .Property(fighter => fighter.InitialCurrentMana)
            .HasColumnName("CombatFighterInitialCurrentMana");
        builder
            .Property(fighter => fighter.InitialMaxMana)
            .HasColumnName("CombatFighterInitialMaxMana");
        builder
            .Property(fighter => fighter.InitialAttack)
            .HasColumnName("CombatFighterInitialAttack");
        builder
            .Property(fighter => fighter.InitialDefense)
            .HasColumnName("CombatFighterInitialDefense");
        builder
            .Property(fighter => fighter.InitialSpeed)
            .HasColumnName("CombatFighterInitialSpeed");
        builder
            .Property(fighter => fighter.AbilitiesJson)
            .HasColumnType("jsonb")
            .HasColumnName("CombatFighterAbilities");
        builder
            .Property(fighter => fighter.ItemsJson)
            .HasColumnType("jsonb")
            .HasColumnName("CombatFighterItems");
        builder
            .Property(fighter => fighter.EffectsJson)
            .HasColumnType("jsonb")
            .HasColumnName("CombatFighterEffects");

        builder.HasIndex(fighter => fighter.CombatId);
    }
}
