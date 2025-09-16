using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Models;
using Application.UseCases.GetProjectVersions;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.UseCases.GetProjectVersions;

public class GetProjectVersionsUseCaseTests
{
    private readonly IVersionRepository _versionRepository;
    private readonly IGetProjectVersionsUseCase _useCase;

    public GetProjectVersionsUseCaseTests()
    {
        _versionRepository = Substitute.For<IVersionRepository>();
        _useCase = new GetProjectVersionsUseCase(_versionRepository);
    }

    [Fact]
    public async Task Run_ShouldReturnVersions_WhenRepositorySucceeds()
    {
        // Arrange
        var projectId = 42L;
        var main = new DomainVersion(1, projectId, "main", "2.0.0.0");
        var feature = new DomainVersion(2, projectId, "feature/x", "1.0.0.1", "meta");
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            [feature.IdentifierName] = feature,
            [main.IdentifierName] = main
        };

        _versionRepository
            .GetCurrentVersions(projectId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(currentVersions));

        var input = new IGetProjectVersionsUseCase.Input(projectId);

        // Act
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        output.ProjectId.ShouldBe(projectId);
        output.Versions.Count.ShouldBe(2);
        output.Versions.ShouldContain(v => v.IdentifierName == "main" && v.ReleaseNumber == "2.0.0.0");
        output.Versions.ShouldContain(v => v.IdentifierName == "feature/x" && v.ReleaseNumber == "1.0.0.1" && v.Meta == "meta");
        output.Versions.Select(v => v.IdentifierName).ShouldBe(new[] { "feature/x", "main" });
    }

    [Fact]
    public async Task Run_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyDictionary()
    {
        // Arrange
        var projectId = 7L;
        _versionRepository
            .GetCurrentVersions(projectId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(new Dictionary<string, DomainVersion>()));

        var input = new IGetProjectVersionsUseCase.Input(projectId);

        // Act
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        output.ProjectId.ShouldBe(projectId);
        output.Versions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_ShouldPropagateError_WhenRepositoryFails()
    {
        // Arrange
        var error = new ApplicationError("RepositoryError", "boom");
        _versionRepository
            .GetCurrentVersions(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Failure(error));

        var input = new IGetProjectVersionsUseCase.Input(99);

        // Act
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
    }
}
