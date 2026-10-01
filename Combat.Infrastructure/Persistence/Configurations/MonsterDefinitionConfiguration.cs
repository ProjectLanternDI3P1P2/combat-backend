using Combat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Combat.Infrastructure.Persistence.Configurations;

public sealed class MonsterDefinitionConfiguration : IEntityTypeConfiguration<MonsterDefinition>
{
    public void Configure(EntityTypeBuilder<MonsterDefinition> builder)
    {
        builder.HasKey(definition => definition.Id);
        builder.Property(definition => definition.Id).HasColumnName("MonsterDefinitionId");
        builder
            .Property(definition => definition.Name)
            .HasMaxLength(100)
            .HasColumnName("MonsterDefinitionName");
        builder
            .Property(definition => definition.Class)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("MonsterDefinitionClass");
        builder
            .Property(definition => definition.ImageUrl)
            .HasMaxLength(300)
            .HasColumnName("MonsterDefinitionImageUrl");
        builder
            .Property(definition => definition.BaseHealth)
            .HasColumnName("MonsterDefinitionBaseHealth");
        builder
            .Property(definition => definition.BaseAttack)
            .HasColumnName("MonsterDefinitionBaseAttack");
        builder
            .Property(definition => definition.BaseDefense)
            .HasColumnName("MonsterDefinitionBaseDefense");
        builder
            .Property(definition => definition.BaseSpeed)
            .HasColumnName("MonsterDefinitionBaseSpeed");
        builder.HasIndex(definition => definition.Class);
    }
}
