using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionBumper
{
    Task<Result<DomainVersion>> CalculateNextVersion(
        string branchName,
        IReadOnlyDictionary<string, DomainVersion> currentVersions,
        CancellationToken cancellationToken = default
    );
}