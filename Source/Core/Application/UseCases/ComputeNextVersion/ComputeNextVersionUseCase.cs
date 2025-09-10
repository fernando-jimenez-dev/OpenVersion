using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
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
        const int maxRetries = 3;
        int attempt = 0;
        Error? error = null;

        while (attempt < maxRetries)
        {
            var currentVersionsResult = await _repository.GetCurrentVersions(input.ProjectId, cancellationToken);
            if (currentVersionsResult.Failed(out var getCurrentVersionsError)) return Failure(getCurrentVersionsError);

            var currentVersions = currentVersionsResult.Value!;
            var nextVersionResult = await _versionBumper.CalculateNextVersion(input.BranchName, input.ProjectId, currentVersions!, input.Context, cancellationToken);
            if (nextVersionResult.Failed(out var calculateNextVersionError)) return Failure(calculateNextVersionError);

            var nextVersion = nextVersionResult.Value!;
            var nextVersionString = string.IsNullOrWhiteSpace(nextVersion.Meta)
                ? nextVersion.ReleaseNumber
                : $"{nextVersion.ReleaseNumber}+{nextVersion.Meta}";

            var saveResult = await _repository.SaveVersion(nextVersion, cancellationToken);
            if (saveResult.IsSuccess)
                return Result.Success(new IComputeNextVersionUseCase.Output(nextVersionString));

            if (saveResult.Failed(out var saveError) && saveError is not VersionConcurrencyError)
                return Failure(saveError);

            attempt++;
            error = saveResult.Error;
        }

        return Failure(error);
    }

    private static Result<IComputeNextVersionUseCase.Output> Failure(Error? error) =>
        Result<IComputeNextVersionUseCase.Output>.Failure(error!);
}
