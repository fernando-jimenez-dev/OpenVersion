using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionBumper
{
    Task<Result<DomainVersion>> CalculateNextVersion(
        string branchName,
        IReadOnlyDictionary<string, DomainVersion> currentVersions,
        IReadOnlyDictionary<string, string?>? context = null,
        CancellationToken cancellationToken = default
    );
}