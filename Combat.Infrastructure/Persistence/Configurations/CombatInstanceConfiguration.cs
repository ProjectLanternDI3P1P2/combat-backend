using Combat.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Combat.Infrastructure.Persistence.Configurations;

public sealed class CombatInstanceConfiguration : IEntityTypeConfiguration<CombatInstanceRecord>
{
    public void Configure(EntityTypeBuilder<CombatInstanceRecord> builder)
    {
        builder.ToTable("CombatInstances");

        builder.HasKey(instance => instance.CombatId);
        builder.Property(instance => instance.CombatId).HasColumnName("CombatId");
        builder.Property(instance => instance.SessionId).HasColumnName("CombatSessionId");
        builder.Property(instance => instance.EncounterId).HasColumnName("CombatEncounterId");
        builder.Property(instance => instance.CommandId).HasColumnName("CombatCommandId");
        builder
            .Property(instance => instance.EncounterType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("CombatEncounterType");
        builder
            .Property(instance => instance.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("CombatStatus");
        builder.Property(instance => instance.StartedAt).HasColumnName("CombatStartedAt");

        // The same command or the same encounter never creates a second combat.
        builder.HasIndex(instance => instance.CommandId).IsUnique();
        builder.HasIndex(instance => instance.EncounterId).IsUnique();
        builder.HasIndex(instance => instance.SessionId);

        builder
            .HasMany(instance => instance.Fighters)
            .WithOne()
            .HasForeignKey(fighter => fighter.CombatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
