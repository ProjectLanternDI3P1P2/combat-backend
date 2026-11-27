using Combat.Domain.Entities;
using Combat.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace Combat.Infrastructure.Persistence;

public class CombatDbContext(DbContextOptions<CombatDbContext> options) : DbContext(options)
{
    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<Monster> Monsters { get; set; }

    public virtual DbSet<MonsterDefinition> MonsterDefinitions { get; set; }

    public virtual DbSet<CombatInstanceRecord> CombatInstances { get; set; }

    public virtual DbSet<CombatFighterRecord> CombatFighters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applique toutes les configurations d'entités automatiquement
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CombatDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
