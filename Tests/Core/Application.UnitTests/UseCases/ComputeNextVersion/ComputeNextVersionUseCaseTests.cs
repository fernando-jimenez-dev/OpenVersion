using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
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

    [Theory]
    [InlineData("main", "1.0.0.0", "", "2.0.0.0")]
    [InlineData("main", "1.0.0.0", "minor", "1.1.0.0")]
    [InlineData("qa", "1.0.0.0", "qa-card-1", "1.0.1.0")]
    [InlineData("feature/card-1", "1.0.0.0", "feature-item-1", "1.0.0.1")]
    [InlineData("fix/card-1", "1.0.0.0", "fix-item-1", "1.0.0.1")]
    public async Task ShouldComputeNextVersion(string branchName, string releaseNumber, string meta, string nextReleaseNumber)
    {
        var currentVersion = new IVersionRepository.Version(
            id: 1,
            projectId: 1,
            identifierName: branchName,
            releaseNumber: releaseNumber,
            lastUpdated: (DateTimeOffset.UtcNow).AddDays(-1)
        );
        var currentVersions = new Dictionary<string, IVersionRepository.Version?>
        {
            { branchName, currentVersion }
        };
        _versionRepository
            .GetCurrentVersions(branchName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>.Success(currentVersions));

        var nextVersion = new IVersionRepository.Version(
            id: currentVersion.Id,
            projectId: currentVersion.ProjectId,
            identifierName: currentVersion.IdentifierName,
            releaseNumber: nextReleaseNumber,
            lastUpdated: DateTimeOffset.UtcNow
        );
        var nextRelease = new IVersionBumper.NextRelease(nextVersion, meta);

        _versionBumper
            .CalculateNextReleaseNumber(branchName, currentVersions!, Arg.Any<CancellationToken>())
            .Returns(Result<IVersionBumper.NextRelease>.Success(nextRelease));

        _versionRepository
            .SaveVersion(nextVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        var expectedNextVersion = string.IsNullOrWhiteSpace(meta) ? nextReleaseNumber : $"{nextReleaseNumber}+{meta}";
        output.NextVersion.ShouldBe(expectedNextVersion);
    }

    [Fact]
    public async Task ShouldComputeNewVersion_WhenPreviousDoesNotExist()
    {
        var branchName = "feature/new-item";
        var currentMainVersion = new IVersionRepository.Version(
            id: 1,
            projectId: 1,
            identifierName: "main",
            releaseNumber: "1.0.0.0",
            lastUpdated: (DateTimeOffset.UtcNow).AddDays(-1)
        );
        var currentVersions = new Dictionary<string, IVersionRepository.Version?>
        {
            { branchName, currentMainVersion }
        };
        _versionRepository
            .GetCurrentVersions(branchName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>.Success(currentVersions));

        var newVersion = new IVersionRepository.Version(
            id: 2,
            projectId: currentMainVersion.ProjectId,
            identifierName: branchName,
            releaseNumber: "1.0.0.1",
            lastUpdated: DateTimeOffset.UtcNow
        );
        var nextRelease = new IVersionBumper.NextRelease(newVersion, "feature-new-item");
        _versionBumper
            .CalculateNextReleaseNumber(branchName, currentVersions!, Arg.Any<CancellationToken>())
            .Returns(Result<IVersionBumper.NextRelease>.Success(nextRelease));

        _versionRepository
            .SaveVersion(newVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        var expectedNextVersion = $"{nextRelease.Version.ReleaseNumber}+{nextRelease.Meta}";
        output.NextVersion.ShouldBe(expectedNextVersion);
    }

    [Fact]
    public async Task ShouldFail_WhenVersionBumperDoesNotRecognizeBranch()
    {
        var branchName = "random-branch";
        var release = new IVersionRepository.Version(
            id: 1,
            projectId: 1,
            identifierName: "main",
            releaseNumber: "1.0.0.0",
            lastUpdated: (DateTimeOffset.UtcNow).AddDays(-1)
        );
        var releases = new Dictionary<string, IVersionRepository.Version?>
        {
            { branchName, release }
        };
        _versionRepository
            .GetCurrentVersions(branchName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>.Success(releases));

        _versionBumper
            .CalculateNextReleaseNumber(branchName, releases!, Arg.Any<CancellationToken>())
            .Returns(Result<IVersionBumper.NextRelease>.Failure(new UnsupportedBranchError(branchName)));

        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldNotBeNull();
        var unsupportedBranchError = error.ShouldBeOfType<UnsupportedBranchError>();
        unsupportedBranchError.BranchName.ShouldBe(branchName);
    }

    [Fact]
    public async Task ShouldFail_WhenGetCurrentVersionsFails()
    {
        _versionRepository
            .GetCurrentVersions(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>
                .Failure(new ApplicationError("unexpected", "Unexpected error")
            ));

        var input = new IComputeNextVersionUseCase.Input("any-branch");
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldNotBeNull();
        var repositoryError = error.ShouldBeOfType<ApplicationError>();
        repositoryError.Type.ShouldBe("unexpected");
        repositoryError.Message.ShouldBe("Unexpected error");
    }

    [Fact]
    public async Task ShouldFail_WhenSavingVersionFails()
    {
        var branchName = "feature/new-item";
        var currentMainVersion = new IVersionRepository.Version(
            id: 1,
            projectId: 1,
            identifierName: "main",
            releaseNumber: "1.0.0.0",
            lastUpdated: (DateTimeOffset.UtcNow).AddDays(-1)
        );
        var currentVersions = new Dictionary<string, IVersionRepository.Version?>
        {
            { branchName, currentMainVersion }
        };
        _versionRepository
            .GetCurrentVersions(branchName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>.Success(currentVersions));

        var newVersion = new IVersionRepository.Version(
            id: 2,
            projectId: currentMainVersion.ProjectId,
            identifierName: branchName,
            releaseNumber: "1.0.0.1",
            lastUpdated: DateTimeOffset.UtcNow
        );
        var nextRelease = new IVersionBumper.NextRelease(newVersion, "feature-new-item");
        _versionBumper
            .CalculateNextReleaseNumber(branchName, currentVersions!, Arg.Any<CancellationToken>())
            .Returns(Result<IVersionBumper.NextRelease>.Success(nextRelease));

        _versionRepository
            .SaveVersion(newVersion, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new ApplicationError("Unexpected", "Something bad happened")));

        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldNotBeNull();
        var repositoryError = error.ShouldBeOfType<ApplicationError>();
        repositoryError.Type.ShouldBe("Unexpected");
        repositoryError.Message.ShouldBe("Something bad happened");
    }
}