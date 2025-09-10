using Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;
using Application.UseCases.ComputeNextVersion.Models;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class FixBumpRuleTests
{
    private readonly FixBumpRule _rule;

    public FixBumpRuleTests()
    {
        _rule = new FixBumpRule();
    }

    [Fact]
    public void ShouldImplementBaseProperties()
    {
        _rule.Priority.ShouldBe(40);
        _rule.Name.ShouldBe(nameof(FixBumpRule));
    }

    [Theory]
    [InlineData("1.0.0.0", "1.0.0.1")]
    [InlineData("1.1.0.0", "1.1.0.1")]
    [InlineData("1.1.1.0", "1.1.1.1")]
    [InlineData("1.1.1.1", "1.1.1.2")]
    [InlineData("0.1.1.1", "0.1.1.2")]
    [InlineData("0.99.1.1", "0.99.1.2")]
    [InlineData("99.99.99.99", "99.99.99.100")]
    public async Task Apply_ShouldBumpVersion(string currentRelease, string expectedNextRelease)
    {
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "fix/item-a", new DomainVersion(1, 1, "fix/item-a", currentRelease) }
        };

        var applyResult = await _rule.Apply("fix/item-a", 1, currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe(expectedNextRelease);
        newVersion.Meta.ShouldBe("fix-item-a");
    }

    [Fact]
    public async Task Apply_ShouldCreateNewVersion_WhenMainDoesNotExist()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var applyResult = await _rule.Apply("fix/item-a", 1, currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe("0.0.0.1");
        newVersion.Meta.ShouldBe("fix-item-a");
    }

    [Fact]
    public async Task Apply_ShouldCreateNewVersion_WhenMainExists()
    {
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", "1.0.0.0") }
        };

        var applyResult = await _rule.Apply("fix/item-a", 1, currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe("1.0.0.1");
        newVersion.Meta.ShouldBe("fix-item-a");
    }

    [Fact]
    public void CanApply_ShouldBeTrueWhenIsQA()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("fix/item-a", currentVersions, context: null);

        canApply.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsNotQA()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("not-fix/item-a", currentVersions, context: null);

        canApply.ShouldBeFalse();
    }
}
