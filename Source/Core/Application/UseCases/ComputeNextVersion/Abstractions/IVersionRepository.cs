using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionRepository
{
    Task<Result<IReadOnlyDictionary<string, DomainVersion>>> GetCurrentVersions(long projectId, CancellationToken cancellationToken = default);

    Task<Result> SaveVersion(DomainVersion version, CancellationToken cancellationToken = default);
}
