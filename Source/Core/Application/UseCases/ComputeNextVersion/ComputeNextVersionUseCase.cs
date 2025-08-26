using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using OpenResult;

namespace Application.UseCases.ComputeNextVersion;

public class ComputeNextVersionUseCase : IComputeNextVersionUseCase
{
    private readonly IVersionRepository _repository;
    private readonly IVersionBumper _versionManager;

    public ComputeNextVersionUseCase(IVersionRepository repository, IVersionBumper versionManager)
    {
        _repository = repository;
        _versionManager = versionManager;
    }

    public async Task<Result<IComputeNextVersionUseCase.Output>> Run(IComputeNextVersionUseCase.Input input, CancellationToken cancellationToken = default)
    {
        var getCurrentVersionsResult = await _repository.GetCurrentVersions(input.BranchName, cancellationToken: cancellationToken);
        if (getCurrentVersionsResult.IsSuccess)
        {
            var currentVersions = getCurrentVersionsResult.Value;
            var calculateNextReleaseResult = await _versionManager.CalculateNextReleaseNumber(
                input.BranchName, currentVersions!, cancellationToken
            );
            if (calculateNextReleaseResult.IsSuccess)
            {
                var nextRelease = calculateNextReleaseResult.Value!;
                var nextFullReleaseNumber = string.IsNullOrWhiteSpace(nextRelease.Meta)
                    ? nextRelease.Version.ReleaseNumber
                    : $"{nextRelease.Version.ReleaseNumber}+{nextRelease.Meta}";

                var saveResult = await _repository.SaveVersion(nextRelease.Version, cancellationToken);
                if (saveResult.IsSuccess)
                {
                    return Result.Success(new IComputeNextVersionUseCase.Output(nextFullReleaseNumber));
                }
                return Fail(saveResult.Error!);
            }

            return Fail(calculateNextReleaseResult.Error!);
        }

        return Fail(getCurrentVersionsResult.Error!);
    }

    private static Result<IComputeNextVersionUseCase.Output> Fail(Error error) => Result<IComputeNextVersionUseCase.Output>.Failure(error);
}