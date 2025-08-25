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
        var getLatestVersionResult = await _repository.GetCurrentReleases(input.BranchName, cancellationToken: cancellationToken);
        if (getLatestVersionResult.IsSuccess)
        {
            var currentReleases = getLatestVersionResult.Value;
            var getNextVersionResult = await _versionManager.CalculateNextReleaseNumber(
                input.BranchName, currentReleases!, cancellationToken
            );
            if (getNextVersionResult.IsSuccess)
            {
                var nextRelease = getNextVersionResult.Value!;
                var nextVersion = string.IsNullOrWhiteSpace(nextRelease.Meta)
                    ? nextRelease.Number
                    : $"{nextRelease.Number}+{nextRelease.Meta}";

                return Result.Success(new IComputeNextVersionUseCase.Output(nextVersion));
            }

            return Fail(getNextVersionResult.Error!);
        }

        return Fail(getLatestVersionResult.Error!);
    }

    private static Result<IComputeNextVersionUseCase.Output> Fail(Error error) => Result<IComputeNextVersionUseCase.Output>.Failure(error);
}