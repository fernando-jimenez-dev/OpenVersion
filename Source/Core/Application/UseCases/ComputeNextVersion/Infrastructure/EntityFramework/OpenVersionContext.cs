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

            // Persist DateTimeOffset as ISO 8601 with UTC offset ('O' round-trip format)
            // and set/update values from application code (no DB defaults)
            entity.Property(e => e.LastUpdated)
                .HasConversion(
                    v => v.ToUniversalTime().ToString("O"),
                    v => DateTimeOffset.Parse(v, null, System.Globalization.DateTimeStyles.RoundtripKind)
                );
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });
    }
}