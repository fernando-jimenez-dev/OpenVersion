using OpenResult;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionRepository
{
    // ProjectId = 1 => using 1 as default for assuming the project is the only one we have now.
    // Does not matter for now, but eventually it could be useful.
    // For the time being the GetLatestVersion would return the only Version Stored available regardless of the projectId.
    Task<Result<Version>> GetLatestVersion(int projectId = 1, CancellationToken cancellationToken = default);

    Task SaveVersion(string branchName, int projectId, CancellationToken cancellationToken = default);

    public record Version()
    {
    }
}