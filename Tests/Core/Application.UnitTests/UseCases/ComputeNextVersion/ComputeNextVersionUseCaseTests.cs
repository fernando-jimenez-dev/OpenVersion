using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
using Application.UseCases.ComputeNextVersion.Models;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion;

public class ComputeNextVersionUseCaseTests
{
    private readonly IVersionBumper _versionBumper;
    private readonly IVersionRepository _versionRepository;
    private readonly IComputeNextVersionUseCase _useCase;

    public ComputeNextVersionUseCaseTests()
    {
        _versionBumper = Substitute.For<IVersionBumper>();
        _versionRepository = Substitute.For<IVersionRepository>();
        _useCase = new ComputeNextVersionUseCase(_versionRepository, _versionBumper);
    }

    #region Success Scenarios

    [Theory]
    [InlineData("main", "1.0.0.0", "", "2.0.0.0")]
    [InlineData("main", "1.0.0.0", "minor", "1.1.0.0")]
    [InlineData("qa", "1.0.0.0", "qa-card-1", "1.0.1.0")]
    [InlineData("feature/card-1", "1.0.0.0", "feature-item-1", "1.0.0.1")]
    [InlineData("fix/card-1", "1.0.0.0", "fix-item-1", "1.0.0.1")]
    public async Task Run_ShouldReturnNextVersion_WhenCurrentVersionExists(
        string branchName, string releaseNumber, string meta, string nextReleaseNumber)
    {
        // Arrange
        var currentVersion = new DomainVersion(1, 1, branchName, releaseNumber);
        var currentVersions = new Dictionary<string, DomainVersion> { { branchName, currentVersion } };
        _versionRepository.GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(currentVersions));

        var nextVersion = new DomainVersion(currentVersion.Id, currentVersion.ProjectId, currentVersion.IdentifierName, nextReleaseNumber, meta);

        _versionBumper
            .CalculateNextVersion(branchName, currentVersions!, Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(nextVersion));
        _versionRepository
            .SaveVersion(nextVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        var expectedNextVersion = string.IsNullOrWhiteSpace(meta) ? nextReleaseNumber : $"{nextReleaseNumber}+{meta}";
        output.NextVersion.ShouldBe(expectedNextVersion);
    }

    [Fact]
    public async Task Run_ShouldReturnNextVersion_WhenPreviousVersionDoesNotExist()
    {
        // Arrange
        var branchName = "feature/new-item";
        var currentMainVersion = new DomainVersion(1, 1, "main", "1.0.0.0");
        var currentVersions = new Dictionary<string, DomainVersion> { { branchName, currentMainVersion } };
        _versionRepository
            .GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(currentVersions));

        var newVersion = new DomainVersion(2, currentMainVersion.ProjectId, branchName, "1.0.0.1", "feature-new-item");

        _versionBumper
            .CalculateNextVersion(branchName, currentVersions!, Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(newVersion));
        _versionRepository
            .SaveVersion(newVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        var expectedNextVersion = $"{newVersion.ReleaseNumber}+{newVersion.Meta}";
        output.NextVersion.ShouldBe(expectedNextVersion);
    }

    [Fact]
    public async Task Run_ShouldRetryOnVersionConcurrencyError_AndSucceed()
    {
        // Arrange
        var branchName = "feature/concurrent";
        var currentMainVersion = new DomainVersion(1, 1, branchName, "1.0.0.0");
        var currentVersions = new Dictionary<string, DomainVersion> { { branchName, currentMainVersion } };
        _versionRepository
            .GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(currentVersions));

        var newVersion = new DomainVersion(2, currentMainVersion.ProjectId, branchName, "1.0.0.1", "concurrent-meta");
        _versionBumper
            .CalculateNextVersion(branchName, currentVersions!, Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(newVersion));

        // Simulate 2 concurrency errors, then success
        int callCount = 0;
        _versionRepository
            .SaveVersion(newVersion, Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount < 3)
                    return Result.Failure(new VersionConcurrencyError(newVersion));
                return Result.Success();
            });

        // Act
        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.NextVersion.ShouldBe($"{newVersion.ReleaseNumber}+{newVersion.Meta}");
    }

    #endregion Success Scenarios

    #region Failure Scenarios

    [Fact]
    public async Task Run_ShouldFail_WhenBranchIsUnsupported()
    {
        // Arrange
        var branchName = "random-branch";
        var release = new DomainVersion(1, 1, "main", "1.0.0.0");
        var releases = new Dictionary<string, DomainVersion?> { { branchName, release } };

        _versionRepository
            .GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(releases));
        _versionBumper
            .CalculateNextVersion(branchName, releases!, Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Failure(new UnsupportedBranchError(branchName)));

        // Act
        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldNotBeNull();
        var unsupportedBranchError = error.ShouldBeOfType<UnsupportedBranchError>();
        unsupportedBranchError.BranchName.ShouldBe(branchName);
    }

    [Fact]
    public async Task Run_ShouldFail_WhenGetCurrentVersionsFails()
    {
        // Arrange
        _versionRepository
            .GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>
                .Failure(new ApplicationError("unexpected", "Unexpected error"))
            );

        // Act
        var input = new IComputeNextVersionUseCase.Input("any-branch");
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldNotBeNull();
        var repositoryError = error.ShouldBeOfType<ApplicationError>();
        repositoryError.Type.ShouldBe("unexpected");
        repositoryError.Message.ShouldBe("Unexpected error");
    }

    [Fact]
    public async Task Run_ShouldFail_WhenSavingVersionFails()
    {
        // Arrange
        var branchName = "feature/new-item";
        var currentMainVersion = new DomainVersion(1, 1, "main", "1.0.0.0");
        var currentVersions = new Dictionary<string, DomainVersion> { { branchName, currentMainVersion } };
        _versionRepository
            .GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(currentVersions));

        var newVersion = new DomainVersion(2, currentMainVersion.ProjectId, branchName, "1.0.0.1", "feature-new-item");

        _versionBumper
            .CalculateNextVersion(branchName, currentVersions!, Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(newVersion));
        _versionRepository
            .SaveVersion(newVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new ApplicationError("Unexpected", "Something bad happened")));

        // Act
        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldNotBeNull();
        var repositoryError = error.ShouldBeOfType<ApplicationError>();
        repositoryError.Type.ShouldBe("Unexpected");
        repositoryError.Message.ShouldBe("Something bad happened");
    }

    [Fact]
    public async Task Run_ShouldFailAfterMaxRetries_OnVersionConcurrencyError()
    {
        // Arrange
        var branchName = "feature/concurrent";
        var currentMainVersion = new DomainVersion(1, 1, branchName, "1.0.0.0");
        var currentVersions = new Dictionary<string, DomainVersion> { { branchName, currentMainVersion } };
        _versionRepository
            .GetCurrentVersions(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, DomainVersion>>.Success(currentVersions));

        var newVersion = new DomainVersion(2, currentMainVersion.ProjectId, branchName, "1.0.0.1", "concurrent-meta");
        _versionBumper
            .CalculateNextVersion(branchName, currentVersions!, Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(newVersion));

        // Always return concurrency error
        _versionRepository
            .SaveVersion(newVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new VersionConcurrencyError(newVersion)));

        // Act
        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeOfType<VersionConcurrencyError>();
    }

    #endregion Failure Scenarios
}