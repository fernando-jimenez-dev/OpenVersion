using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class MainMajorBumpRule : IVersionRule
{
    public int Priority => 10;
    public string Name => nameof(MainMajorBumpRule);

    public bool CanApply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context)
        => branchName == "main" && IsTrue(context, "isMajor");

    public Task<Result<DomainVersion>> Apply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context, CancellationToken cancellationToken = default)
    {
        if (!currentVersions.TryGetValue("main", out var current))
        {
            var initial = new DomainVersion(0, 1, "main", "1.0.0.0");
            return Task.FromResult(Result<DomainVersion>.Success(initial));
        }

        var major = Parse(current.ReleaseNumber);
        var next = new DomainVersion(0, current.ProjectId, branchName, $"{major + 1}.0.0.0");
        return Task.FromResult(Result<DomainVersion>.Success(next));
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string?>? context, string key)
        => context != null && context.TryGetValue(key, out var v) && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);

    private static int Parse(string version)
    {
        var parts = version.Split('.');
        return int.Parse(parts[0]);
    }
}