using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;

public class OpenVersionContext : DbContext
{
    public DbSet<VersionEntity> Versions => Set<VersionEntity>();

    public OpenVersionContext(DbContextOptions<OpenVersionContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VersionEntity>(entity =>
        {
            entity.ToTable("Versions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.IdentifierName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReleaseNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Meta).HasMaxLength(200);

            entity.HasIndex(i => new { i.ProjectId, i.IdentifierName }).IsUnique();

            entity.Property(e => e.LastUpdated).HasDefaultValue(DateTimeOffset.UtcNow).ValueGeneratedOnAddOrUpdate();
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });
    }
}