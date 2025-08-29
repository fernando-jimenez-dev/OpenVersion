using Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;
using Application.UseCases.ComputeNextVersion.Models;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class MainMinorBumpRuleTests
{
    private readonly MainMinorBumpRule _rule;

    public MainMinorBumpRuleTests()
    {
        _rule = new MainMinorBumpRule();
    }

    [Fact]
    public void ShouldImplementBaseProperties()
    {
        _rule.Priority.ShouldBe(15);
        _rule.Name.ShouldBe(nameof(MainMinorBumpRule));
    }

    [Theory]
    [InlineData("1.0.0.0", "1.1.0.0")]
    [InlineData("1.1.0.0", "1.2.0.0")]
    [InlineData("1.1.1.0", "1.2.0.0")]
    [InlineData("1.1.1.1", "1.2.0.0")]
    [InlineData("0.1.1.1", "0.2.0.0")]
    [InlineData("0.99.1.1", "0.100.0.0")]
    [InlineData("99.99.99.99", "99.100.0.0")]
    public async Task Apply_ShouldBumpVersion(string currentRelease, string expectedNextRelease)
    {
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", currentRelease) }
        };

        var applyResult = await _rule.Apply("main", currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe(expectedNextRelease);
        newVersion.Meta.ShouldBe("minor");
    }

    [Fact]
    public async Task Apply_ShouldCreateNewVersion()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var applyResult = await _rule.Apply("main", currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe("0.1.0.0");
        newVersion.Meta.ShouldBe("minor");
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsMain_AndMarkedAsMajor()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();
        var context = new Dictionary<string, string?>
        {
            { "isMajor", "true" }
        };

        var canApply = _rule.CanApply("main", currentVersions, context);

        canApply.ShouldBeFalse();
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsNotMain()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("not-main", currentVersions, context: null);

        canApply.ShouldBeFalse();
    }

    [Fact]
    public void CanApply_ShouldBeTrueWhenIsMain_AndIsNotMajor()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();
        var emptyContext = new Dictionary<string, string?>();

        var canApply = _rule.CanApply("main", currentVersions, emptyContext);

        canApply.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_ShouldBeTrueWhenIsMain_AndContextIsNull()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("main", currentVersions, null);

        canApply.ShouldBeTrue();
    }
}