using System.Collections.Generic;

namespace WebAPI.Minimal.UseCases.GetProjectVersions;

public record GetProjectVersionsResponse(long ProjectId, IReadOnlyList<GetProjectVersionsResponse.ProjectVersionItem> Versions)
{
    public record ProjectVersionItem(long Id, string IdentifierName, string ReleaseNumber, string? Meta);
}

public record GetProjectVersionsErrorResponse(string Message);
