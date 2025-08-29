using Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;
using Application.UseCases.ComputeNextVersion.Models;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class QaBumpRuleTests
{
    private readonly QaBumpRule _rule;

    public QaBumpRuleTests()
    {
        _rule = new QaBumpRule();
    }

    [Fact]
    public void ShouldImplementBaseProperties()
    {
        _rule.Priority.ShouldBe(20);
        _rule.Name.ShouldBe(nameof(QaBumpRule));
    }

    [Theory]
    [InlineData("1.0.0.0", "1.0.1.0")]
    [InlineData("1.1.0.0", "1.1.1.0")]
    [InlineData("1.1.1.0", "1.1.2.0")]
    [InlineData("1.1.1.1", "1.1.2.0")]
    [InlineData("0.1.1.1", "0.1.2.0")]
    [InlineData("0.99.1.1", "0.99.2.0")]
    [InlineData("99.99.99.99", "99.99.100.0")]
    public async Task Apply_ShouldBumpVersion(string currentRelease, string expectedNextRelease)
    {
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "qa", new DomainVersion(1, 1, "qa", currentRelease) }
        };

        var applyResult = await _rule.Apply("qa", currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe(expectedNextRelease);
        newVersion.Meta.ShouldBe("qa");
    }

    [Fact]
    public async Task Apply_ShouldCreateNewVersion_WhenMainDoesNotExist()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var applyResult = await _rule.Apply("qa", currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe("0.0.1.0");
        newVersion.Meta.ShouldBe("qa");
    }

    [Fact]
    public async Task Apply_ShouldCreateNewVersion_WhenMainExists()
    {
        var currentVersions = new Dictionary<string, DomainVersion>
        {
            { "main", new DomainVersion(1, 1, "main", "1.0.0.0") }
        };

        var applyResult = await _rule.Apply("qa", currentVersions, context: null);

        applyResult.Succeeded(out var newVersion).ShouldBeTrue();
        newVersion.ShouldNotBeNull();
        newVersion.ReleaseNumber.ShouldBe("1.0.1.0");
        newVersion.Meta.ShouldBe("qa");
    }

    [Fact]
    public void CanApply_ShouldBeTrueWhenIsQA()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("qa", currentVersions, context: null);

        canApply.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_ShouldBeFalseWhenIsNotQA()
    {
        var currentVersions = new Dictionary<string, DomainVersion>();

        var canApply = _rule.CanApply("not-qa", currentVersions, context: null);

        canApply.ShouldBeFalse();
    }
}