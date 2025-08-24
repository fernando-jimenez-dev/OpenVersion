using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion;

public class UseCaseTests
{
    private readonly IVersionManager _versionManager;
    private readonly IVersionRepository _repository;
    private readonly IComputeNextVersionUseCase _useCase;

    public UseCaseTests()
    {
        _versionManager = Substitute.For<IVersionManager>();
        _repository = Substitute.For<IVersionRepository>();
        _useCase = new UseCase(_repository, _versionManager);
    }

    [Fact]
    public async Task ShouldRun()
    {
        _repository.GetLatestVersion();
        var input = new IComputeNextVersionUseCase.Input("feature/kanban-item-1");
        var result = await _useCase.Run(input, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        output.NextVersion.ShouldBe("1.0.0.2+feature-kanban-item-1");
    }
}