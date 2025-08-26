using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using OpenResult;

namespace Application.UseCases.ComputeNextVersion;

public class ComputeNextVersionUseCase : IComputeNextVersionUseCase
{
    private readonly IVersionRepository _repository;
    private readonly IVersionBumper _versionBumper;

    public ComputeNextVersionUseCase(IVersionRepository repository, IVersionBumper versionBumper)
    {
        _repository = repository;
        _versionBumper = versionBumper;
    }

    public async Task<Result<IComputeNextVersionUseCase.Output>> Run(
        IComputeNextVersionUseCase.Input input,
        CancellationToken cancellationToken = default)
    {
        var currentVersionsResult = await _repository.GetCurrentVersions(input.BranchName, cancellationToken);
        if (currentVersionsResult.Failed(out var getCurrentVersionsError)) return Failure(getCurrentVersionsError);

        var currentVersions = currentVersionsResult.Value!;
        var nextReleaseResult = await _versionBumper.CalculateNextReleaseNumber(input.BranchName, currentVersions!, cancellationToken);
        if (nextReleaseResult.Failed(out var calculateNextReleaseError)) return Failure(calculateNextReleaseError);

        var nextRelease = nextReleaseResult.Value!;
        var nextVersionString = string.IsNullOrWhiteSpace(nextRelease.Meta)
            ? nextRelease.Version.ReleaseNumber
            : $"{nextRelease.Version.ReleaseNumber}+{nextRelease.Meta}";

        var saveResult = await _repository.SaveVersion(nextRelease.Version, cancellationToken);
        if (saveResult.Failed(out var saveVersionError)) return Failure(saveVersionError);

        return Result.Success(new IComputeNextVersionUseCase.Output(nextVersionString));
    }

    private static Result<IComputeNextVersionUseCase.Output> Failure(Error? error) =>
        Result<IComputeNextVersionUseCase.Output>.Failure(error!);
}