namespace Application.UseCases.ComputeNextVersion.Models;

public class DomainVersion
{
    public long Id { get; } // Database Id
    public long ProjectId { get; } // Project Id foreign key
    public string IdentifierName { get; } // e.g. "main" or "feature/kanban-item-1" or "qa
    public string ReleaseNumber { get; } // e.g. "1.0.0.0"
    public DateTimeOffset LastUpdated { get; }
    public string? Meta { get; set; }

    public DomainVersion(
        long id,
        long projectId,
        string identifierName,
        string releaseNumber,
        DateTimeOffset lastUpdated,
        string? meta = null)
    {
        Id = id;
        ProjectId = projectId;
        IdentifierName = identifierName;
        ReleaseNumber = releaseNumber;
        LastUpdated = lastUpdated;
        Meta = meta;
    }
}