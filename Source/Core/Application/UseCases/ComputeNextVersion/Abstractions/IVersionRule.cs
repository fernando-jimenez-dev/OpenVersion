using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionRule
{
    int Priority { get; }
    string Name { get; }

    bool CanApply(
        string branchName,
        IReadOnlyDictionary<string, DomainVersion> currentVersions,
        IReadOnlyDictionary<string, string?>? context);

    Task<Result<DomainVersion>> Apply(
        string branchName,
        IReadOnlyDictionary<string, DomainVersion> currentVersions,
        IReadOnlyDictionary<string, string?>? context,
        CancellationToken cancellationToken = default);
}