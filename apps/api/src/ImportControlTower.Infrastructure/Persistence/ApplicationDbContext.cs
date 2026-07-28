using ImportControlTower.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<SystemMigrationHistory> SystemMigrations => Set<SystemMigrationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SystemMigrationHistory>(entity =>
        {
            entity.ToTable("system_migrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MigrationName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AppliedAtUtc).IsRequired();
        });
    }
}
