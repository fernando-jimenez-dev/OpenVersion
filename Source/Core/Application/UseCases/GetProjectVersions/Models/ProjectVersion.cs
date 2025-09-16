using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.GetProjectVersions.Models;

public record ProjectVersion(long Id, long ProjectId, string IdentifierName, string ReleaseNumber, string? Meta)
{
    public static ProjectVersion FromDomain(DomainVersion domain) =>
        new(domain.Id, domain.ProjectId, domain.IdentifierName, domain.ReleaseNumber, domain.Meta);
}
