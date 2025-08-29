namespace Application.UseCases.ComputeNextVersion.Models;

public class DomainVersion
{
    public long Id { get; }
    public long ProjectId { get; }
    public string IdentifierName { get; }
    public string ReleaseNumber { get; }
    public string? Meta { get; }

    public DomainVersion(
        long id,
        long projectId,
        string identifierName,
        string releaseNumber,
        string? meta = null)
    {
        Id = id;
        ProjectId = projectId;
        IdentifierName = identifierName;
        ReleaseNumber = releaseNumber;
        Meta = meta;
    }

    // Domain methods for creating new versions
    public DomainVersion WithNewRelease(string newReleaseNumber) =>
        new DomainVersion(Id, ProjectId, IdentifierName, newReleaseNumber, Meta);

    public DomainVersion WithMeta(string meta) =>
        new DomainVersion(Id, ProjectId, IdentifierName, ReleaseNumber, meta);
}