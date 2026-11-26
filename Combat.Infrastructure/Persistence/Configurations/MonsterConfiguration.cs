using Combat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Combat.Infrastructure.Persistence.Configurations;

public sealed class MonsterConfiguration : IEntityTypeConfiguration<Monster>
{
    public void Configure(EntityTypeBuilder<Monster> builder)
    {
        builder.HasKey(monster => monster.Id);
        builder.Property(monster => monster.Id).HasColumnName("MonsterId");
        builder.Property(monster => monster.DungeonRunId).HasColumnName("MonsterDungeonRunId");
        builder.Property(monster => monster.IdempotencyKey).HasColumnName("MonsterIdempotencyKey");
        builder
            .Property(monster => monster.MonsterDefinitionId)
            .HasColumnName("MonsterDefinitionId");
        builder.Property(monster => monster.Name).HasMaxLength(100).HasColumnName("MonsterName");
        builder
            .Property(monster => monster.Class)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("MonsterClass");
        builder.Property(monster => monster.Level).HasColumnName("MonsterLevel");
        builder.Property(monster => monster.PlayerCount).HasColumnName("MonsterPlayerCount");
        builder
            .Property(monster => monster.ImageUrl)
            .HasMaxLength(300)
            .HasColumnName("MonsterImageUrl");
        builder.Property(monster => monster.BaseHealth).HasColumnName("MonsterBaseHealth");
        builder.Property(monster => monster.BaseAttack).HasColumnName("MonsterBaseAttack");
        builder.Property(monster => monster.BaseDefense).HasColumnName("MonsterBaseDefense");
        builder.Property(monster => monster.BaseSpeed).HasColumnName("MonsterBaseSpeed");
        builder.HasIndex(monster => monster.IdempotencyKey).IsUnique();
        builder
            .HasOne(monster => monster.MonsterDefinition)
            .WithMany()
            .HasForeignKey(monster => monster.MonsterDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
