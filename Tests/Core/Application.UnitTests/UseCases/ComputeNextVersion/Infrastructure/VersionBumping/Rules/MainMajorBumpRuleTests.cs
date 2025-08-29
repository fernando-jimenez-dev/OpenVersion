using Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;
using Application.UseCases.ComputeNextVersion.Models;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class MainMajorBumpRuleTests
{
    private readonly MainMajorBumpRule _rule;

    public MainMajorBumpRuleTests()
    {
        _rule = new MainMajorBumpRule();
    }

    [Fact]
    public void ShouldImplementBaseProperties()
    {
        _rule.Priority.ShouldBe(10);
        _rule.Name.ShouldBe(nameof(MainMajorBumpRule));
    }

    [Theory]
    [InlineData("1.0.0.0", "2.0.0.0")]
    [InlineData("1.1.0.0", "2.0.0.0")]
    [InlineData("1.1.1.0", "2.0.0.0")]
    [InlineData("1.1.1.1", "2.0.0.0")]
    [InlineData("0.1.1.1", "1.0.0.0")]
    [InlineData("0.99.1.1", "1.0.0.0")]
    [InlineData("99.99.99.99", "100.0.0.0")]
    public async Task Apply_ShouldBumpVersion(string currentRelease, string expectedNextRelease)
    {
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", currentRelease) }
        };
        var context = new Dictionary<string, string?>
        {
            { "isMajor", "true" }
        };

        var applyResult = await _rule.Apply("main", currentVersions, context);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe(expectedNextRelease);
        string.IsNullOrWhiteSpace(newVersion.Meta).ShouldBeTrue();
    }

    [Fact]
    public async Task Apply_ShouldCreateNewVersion()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();
        var context = new Dictionary<string, string?>
        {
            { "isMajor", "true" }
        };

        var applyResult = await _rule.Apply("main", currentVersions, context);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe("1.0.0.0");
        string.IsNullOrWhiteSpace(newVersion.Meta).ShouldBeTrue();
    }

    [Fact]
    public void CanApply_ShouldBeTrueWhenIsMain_AndMarkedAsMajor()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();
        var context = new Dictionary<string, string?>
        {
            { "isMajor", "true" }
        };

        var canApply = _rule.CanApply("main", currentVersions, context);

        canApply.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsNotMain()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();
        var context = new Dictionary<string, string?>
        {
            { "isMajor", "true" }
        };

        var canApply = _rule.CanApply("not-main", currentVersions, context);

        canApply.ShouldBeFalse();
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsMain_AndIsNotMajor()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();
        var emptyContext = new Dictionary<string, string?>();

        var canApply = _rule.CanApply("main", currentVersions, emptyContext);

        canApply.ShouldBeFalse();
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsMain_AndContextIsNull()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("main", currentVersions, null);

        canApply.ShouldBeFalse();
    }
}