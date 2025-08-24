using Application.UseCases.ComputeNextVersion.Abstractions;
using OpenResult;

namespace Application.UseCases.ComputeNextVersion;

public class UseCase : IComputeNextVersionUseCase
{
    private readonly IVersionRepository _repository;
    private readonly IVersionManager _versionManager;

    public UseCase(IVersionRepository repository, IVersionManager versionManager)
    {
        _repository = repository;
        _versionManager = versionManager;
    }

    public Task<Result<IComputeNextVersionUseCase.Output>> Run(IComputeNextVersionUseCase.Input input, CancellationToken cancellationToken = default)
    {
    }
}