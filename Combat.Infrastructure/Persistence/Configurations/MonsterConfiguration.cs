using Combat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Combat.Infrastructure.Persistence.Configurations;

public class MonsterConfiguration : IEntityTypeConfiguration<Monster>
{
    public void Configure(EntityTypeBuilder<Monster> builder)
    {
        builder.HasKey(m => m.MonsterId);

        builder.Property(m => m.MonsterId)
            .IsRequired()
            .HasColumnName("MonsterId");

        builder.Property(m => m.CombatId)
            .IsRequired()
            .HasColumnName("MonsterCombatId");

        builder.Property(m => m.IsBoss)
            .IsRequired()
            .HasColumnName("MonsterIsBoss");

        builder.Property(m => m.BaseHp)
            .IsRequired()
            .HasColumnName("MonsterBaseHp");

        builder.Property(m => m.BaseAttack)
            .IsRequired()
            .HasColumnName("MonsterBaseAttack");

        builder.Property(m => m.BaseDefense)
            .IsRequired()
            .HasColumnName("MonsterBaseDefense");

        builder.Property(m => m.BaseSpeed)
            .IsRequired()
            .HasColumnName("MonsterBaseSpeed");

        builder.Property(m => m.State)
            .IsRequired()
            .HasColumnName("MonsterState")
            .HasConversion<string>();
    }
}
