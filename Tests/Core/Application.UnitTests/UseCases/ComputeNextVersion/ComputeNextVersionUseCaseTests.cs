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
        var release = new IVersionRepository.Version(
            id: 1,
            projectId: 1,
            identifierName: branchName,
            releaseNumber: releaseNumber,
            lastUpdated: (DateTimeOffset.UtcNow).AddDays(-1)
        );
        var releases = new Dictionary<string, IVersionRepository.Version?>
        {
            { branchName, release }
        };
        _versionRepository
            .GetCurrentReleases(branchName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>.Success(releases));

        var nextRelease = new IVersionBumper.NextRelease(nextReleaseNumber, meta);
        _versionBumper
            .CalculateNextReleaseNumber(branchName, releases!, Arg.Any<CancellationToken>())
            .Returns(Result<IVersionBumper.NextRelease>.Success(nextRelease));

        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        var expectedNextVersion = string.IsNullOrWhiteSpace(meta) ? nextReleaseNumber : $"{nextReleaseNumber}+{meta}";
        output.NextVersion.ShouldBe(expectedNextVersion);
    }

    [Fact]
    public async Task ShouldComputeNewVersionWhenPreviousDoesNotExist()
    {
        var branchName = "feature/new-item";
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
            .GetCurrentReleases(branchName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyDictionary<string, IVersionRepository.Version?>>.Success(releases));

        var nextRelease = new IVersionBumper.NextRelease("1.0.0.1", "feature-new-item");
        _versionBumper
            .CalculateNextReleaseNumber(branchName, releases!, Arg.Any<CancellationToken>())
            .Returns(Result<IVersionBumper.NextRelease>.Success(nextRelease));

        var input = new IComputeNextVersionUseCase.Input(branchName);
        var result = await _useCase.Run(input);

        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ShouldNotBeNull();
        var expectedNextVersion = $"{nextRelease.Number}+{nextRelease.Meta}";
        output.NextVersion.ShouldBe(expectedNextVersion);
    }

    [Fact]
    public async Task ShouldFailWhenVersionBumperDoesNotRecognizeBranch()
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
            .GetCurrentReleases(branchName, cancellationToken: Arg.Any<CancellationToken>())
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
    public async Task ShouldFailWhenVersionRepositoryFails()
    {
        _versionRepository
            .GetCurrentReleases(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
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
}