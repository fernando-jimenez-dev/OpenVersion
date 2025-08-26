using Application.Shared.OpenResult;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionRepository
{
    // ProjectId = 1 => using 1 as default for assuming the project is the only one we have now.
    // Does not matter for now, but eventually it could be useful.
    // For the time being the GetLatestVersion would return the only Version Stored available regardless of the projectId.
    Task<Result<IReadOnlyDictionary<string, Version?>>> GetCurrentVersions(string branchName, CancellationToken cancellationToken = default);

    Task<Result> SaveVersion(Version version, CancellationToken cancellationToken = default);

    public class Version
    {
        public long Id { get; } // Database Id
        public long ProjectId { get; } // Project Id foreign key
        public string IdentifierName { get; } // e.g. "main" or "feature/kanban-item-1" or "qa
        public string ReleaseNumber { get; } // e.g. "1.0.0.0"

        public DateTimeOffset LastUpdated { get; }

        public Version(long id, long projectId, string identifierName, string releaseNumber, DateTimeOffset lastUpdated)
        {
            Id = id;
            ProjectId = projectId;
            IdentifierName = identifierName;
            ReleaseNumber = releaseNumber;
            LastUpdated = lastUpdated;
        }
    }
}