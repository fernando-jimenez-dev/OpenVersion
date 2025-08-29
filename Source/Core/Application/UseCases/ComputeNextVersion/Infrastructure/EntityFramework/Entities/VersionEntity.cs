using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.Entities;

public class VersionEntity
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string IdentifierName { get; set; } = string.Empty;
    public string ReleaseNumber { get; set; } = string.Empty;
    public string? Meta { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public Guid ConcurrencyToken { get; set; }

    // Static factory methods for creating new entities
    /// <summary>
    /// Creates a new VersionEntity from a domain model.
    /// For new entities, Id should be 0 to let EF Core generate a new identity.
    /// For existing entities, Id should contain the existing identity.
    /// </summary>
    /// <param name="domain">The domain model to map from</param>
    /// <returns>A new entity ready for insertion or update</returns>
    public static VersionEntity CreateFromDomain(DomainVersion domain) =>
        new()
        {
            Id = domain.Id, // Use the domain model's Id directly
            ProjectId = domain.ProjectId,
            IdentifierName = domain.IdentifierName,
            ReleaseNumber = domain.ReleaseNumber,
            Meta = domain.Meta,
            LastUpdated = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };

    // Instance method for updating existing entity
    /// <summary>
    /// Updates this entity's properties from a domain model.
    /// Automatically sets LastUpdated and generates a new ConcurrencyToken.
    /// </summary>
    /// <param name="domain">The domain model containing the new values</param>
    public void UpdateFrom(DomainVersion domain)
    {
        // Note: Id should not change during updates
        ReleaseNumber = domain.ReleaseNumber;
        Meta = domain.Meta;
        LastUpdated = DateTimeOffset.UtcNow;
        ConcurrencyToken = Guid.NewGuid();
    }

    // Mapping back to domain model
    /// <summary>
    /// Maps this entity back to a domain model.
    /// </summary>
    /// <returns>A new DomainVersion instance</returns>
    public DomainVersion ToDomain() =>
        new DomainVersion(Id, ProjectId, IdentifierName, ReleaseNumber, Meta);
}