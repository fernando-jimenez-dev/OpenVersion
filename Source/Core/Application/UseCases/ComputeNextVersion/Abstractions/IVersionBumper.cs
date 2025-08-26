using Application.Shared.OpenResult;

namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionBumper
{
    Task<Result<NextRelease>> CalculateNextReleaseNumber(
        string branchName,
        IReadOnlyDictionary<string, IVersionRepository.Version> currentReleases,
        CancellationToken cancellationToken = default
    );

    public record NextRelease(IVersionRepository.Version Version, string? Meta = null);
}