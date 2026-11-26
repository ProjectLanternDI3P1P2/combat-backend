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
        builder.Property(instance => instance.RunId).HasColumnName("CombatRunId");
        builder.Property(instance => instance.Floor).HasColumnName("CombatFloor");
        builder.Property(instance => instance.RoomId).HasColumnName("CombatRoomId");
        builder
            .Property(instance => instance.Result)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("CombatResult");
        builder.Property(instance => instance.Round).HasColumnName("CombatRound");
        builder.Property(instance => instance.Turn).HasColumnName("CombatTurn");
        builder.Property(instance => instance.RoundIndex).HasColumnName("CombatRoundIndex");
        builder
            .Property(instance => instance.RoundOrderJson)
            .HasColumnType("jsonb")
            .HasColumnName("CombatRoundOrder");
        builder
            .Property(instance => instance.ActiveFighterId)
            .HasColumnName("CombatActiveFighterId");
        builder.Property(instance => instance.TurnDeadline).HasColumnName("CombatTurnDeadline");
        builder
            .Property(instance => instance.TurnDurationSeconds)
            .HasColumnName("CombatTurnDurationSeconds");
        builder.Property(instance => instance.EndedAt).HasColumnName("CombatEndedAt");
        builder
            .Property(instance => instance.ResultReportedAt)
            .HasColumnName("CombatResultReportedAt");
        builder
            .Property(instance => instance.EventsJson)
            .HasColumnType("jsonb")
            .HasColumnName("CombatEvents");
        builder
            .Property(instance => instance.ProcessedCommandsJson)
            .HasColumnType("jsonb")
            .HasColumnName("CombatProcessedCommands");
        builder.Property(instance => instance.Version).IsRowVersion();

        // The same command or the same encounter never creates a second combat.
        builder.HasIndex(instance => instance.CommandId).IsUnique();
        builder.HasIndex(instance => instance.EncounterId).IsUnique();
        builder.HasIndex(instance => instance.SessionId);
        builder.HasIndex(instance => instance.RunId);
        // Finished combats still waiting for Dungeon to acknowledge their result.
        builder
            .HasIndex(instance => instance.EndedAt)
            .HasFilter("\"CombatResultReportedAt\" IS NULL AND \"CombatEndedAt\" IS NOT NULL");

        builder
            .HasMany(instance => instance.Fighters)
            .WithOne()
            .HasForeignKey(fighter => fighter.CombatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
