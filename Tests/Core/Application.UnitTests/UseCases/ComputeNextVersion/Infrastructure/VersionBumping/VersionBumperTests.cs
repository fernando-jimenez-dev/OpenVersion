using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
using Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping;
using Application.UseCases.ComputeNextVersion.Models;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion.Infrastructure.VersionBumping;

public class VersionBumperTests
{
    #region Success Scenarios

    [Fact]
    public async Task CalculateNextVersion_ShouldExecuteFirstApplicableRule_WhenMultipleRulesCanApply()
    {
        // Arrange
        var rule1 = Substitute.For<IVersionRule>();
        rule1.Priority.Returns(10);
        rule1.Name.Returns("Rule1");
        rule1.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(false);

        var rule2 = Substitute.For<IVersionRule>();
        rule2.Priority.Returns(20);
        rule2.Name.Returns("Rule2");
        rule2.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var rule3 = Substitute.For<IVersionRule>();
        rule3.Priority.Returns(30);
        rule3.Name.Returns("Rule3");
        rule3.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var versionBumper = new VersionBumper([rule1, rule2, rule3]);

        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", "1.0.0.0") }
        };

        var expectedVersion = new DomainVersion(0, 1, "main", "2.0.0.0");
        rule2.Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(expectedVersion));

        // Act
        var result = await versionBumper.CalculateNextVersion("main", 1, currentVersions);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedVersion);

        // Verify rule2 was called (first applicable rule by priority)
        await rule2.Received(1).Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>());

        // Verify rule3 was never called (rule2 was selected first)
        await rule3.DidNotReceive().Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateNextVersion_ShouldExecutePriority_WhenMultipleRulesCanApply()
    {
        // Arrange
        var rule1 = Substitute.For<IVersionRule>();
        rule1.Priority.Returns(10);
        rule1.Name.Returns("Rule1");
        rule1.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var rule2 = Substitute.For<IVersionRule>();
        rule2.Priority.Returns(20);
        rule2.Name.Returns("Rule2");
        rule2.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var rule3 = Substitute.For<IVersionRule>();
        rule3.Priority.Returns(30);
        rule3.Name.Returns("Rule3");
        rule3.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var versionBumper = new VersionBumper([rule1, rule2, rule3]);

        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", "1.0.0.0") }
        };

        var expectedVersion = new DomainVersion(0, 1, "main", "2.0.0.0");
        rule1.Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(expectedVersion));

        // Act
        var result = await versionBumper.CalculateNextVersion("main", 1, currentVersions);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedVersion);

        // Verify rule1 was called (by priority)
        await rule2.DidNotReceive().Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>());
        await rule3.DidNotReceive().Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>());

        // Verify rule2 and rule3 was never called (rule1 was selected first)
        await rule3.DidNotReceive().Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateNextVersion_ShouldPassCorrectParameters_ToSelectedRule()
    {
        // Arrange
        var rule = Substitute.For<IVersionRule>();
        rule.Priority.Returns(10);
        rule.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var versionBumper = new VersionBumper([rule]);

        var branchName = "feature/test";
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", "1.0.0.0") }
        };
        var context = new Dictionary<string, string?> { { "key", "value" } };

        var expectedVersion = new DomainVersion(0, 1, branchName, "1.0.0.1");
        rule.Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(expectedVersion));

        // Act
        var result = await versionBumper.CalculateNextVersion(branchName, 1, currentVersions, context);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify rule received correct parameters
        rule.Received(1).CanApply(branchName, currentVersions, context);
        await rule.Received(1).Apply(branchName, 1, currentVersions, context, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateNextVersion_ShouldReturnRuleResult_WhenRuleSucceeds()
    {
        // Arrange
        var rule = Substitute.For<IVersionRule>();
        rule.Priority.Returns(10);
        rule.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var versionBumper = new VersionBumper([rule]);

        var currentVersions = new Dictionary<string, DomainVersion>();
        var expectedVersion = new DomainVersion(0, 1, "main", "1.0.0.0");
        rule.Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Success(expectedVersion));

        // Act
        var result = await versionBumper.CalculateNextVersion("main", 1, currentVersions);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedVersion);
    }

    [Fact]
    public async Task CalculateNextVersion_ShouldReturnRuleFailure_WhenRuleFails()
    {
        // Arrange
        var rule = Substitute.For<IVersionRule>();
        rule.Priority.Returns(10);
        rule.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(true);

        var versionBumper = new VersionBumper([rule]);

        var currentVersions = new Dictionary<string, DomainVersion>();
        var expectedError = new ApplicationError("RuleError", "Rule failed");
        rule.Apply(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainVersion>.Failure(expectedError));

        // Act
        var result = await versionBumper.CalculateNextVersion("main", 1, currentVersions);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(expectedError);
    }

    #endregion Success Scenarios

    #region Failure Scenarios

    [Fact]
    public async Task CalculateNextVersion_ShouldReturnUnsupportedBranchError_WhenNoRuleCanApply()
    {
        // Arrange
        var rule1 = Substitute.For<IVersionRule>();
        rule1.Priority.Returns(10);
        rule1.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(false);

        var rule2 = Substitute.For<IVersionRule>();
        rule2.Priority.Returns(20);
        rule2.CanApply(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, DomainVersion>>(), Arg.Any<IReadOnlyDictionary<string, string?>?>())
            .Returns(false);

        var versionBumper = new VersionBumper([rule1, rule2]);

        var currentVersions = new Dictionary<string, DomainVersion>();
        var branchName = "unsupported-branch";

        // Act
        var result = await versionBumper.CalculateNextVersion(branchName, 1, currentVersions);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldBeOfType<UnsupportedBranchError>();
        error.BranchName.ShouldBe(branchName);
    }

    #endregion Failure Scenarios
}



