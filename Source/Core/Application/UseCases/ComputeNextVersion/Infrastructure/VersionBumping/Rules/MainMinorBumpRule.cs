using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class MainMinorBumpRule : IVersionRule
{
    public int Priority => 15; // after major rule
    public string Name => nameof(MainMinorBumpRule);

    public bool CanApply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context)
        => branchName == "main" && !IsTrue(context, "isMajor");

    public Task<Result<DomainVersion>> Apply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context, CancellationToken cancellationToken = default)
    {
        if (!currentVersions.TryGetValue("main", out var current))
        {
            var initial = new DomainVersion(0, 1, "main", "0.1.0.0", "minor");
            return Task.FromResult(Result<DomainVersion>.Success(initial));
        }

        var (major, minor) = Parse(current.ReleaseNumber);
        var next = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor + 1}.0.0", "minor");
        return Task.FromResult(Result<DomainVersion>.Success(next));
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string?>? context, string key)
        => context != null && context.TryGetValue(key, out var v) && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);

    private static (int major, int minor) Parse(string version)
    {
        var parts = version.Split('.');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }
}