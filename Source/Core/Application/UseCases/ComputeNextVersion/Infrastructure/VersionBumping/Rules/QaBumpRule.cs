using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

public class QaBumpRule : IVersionRule
{
    public int Priority => 20;
    public string Name => nameof(QaBumpRule);

    public bool CanApply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context)
        => branchName.StartsWith("qa");

    public Task<Result<DomainVersion>> Apply(string branchName, long projectId, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context, CancellationToken cancellationToken = default)
    {
        if (!TryGetBaseVersion(branchName, currentVersions, out var current))
        {
            // Initial default when nothing exists
            var initial = new DomainVersion(0, projectId, branchName, "0.0.1.0", "qa");
            return Task.FromResult(Result<DomainVersion>.Success(initial));
        }

        var (major, minor, qa) = Parse(current.ReleaseNumber);
        var next = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor}.{qa + 1}.0", "qa");
        return Task.FromResult(Result<DomainVersion>.Success(next));
    }

    private static bool TryGetBaseVersion(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, out DomainVersion current)
    {
        if (currentVersions.TryGetValue(branchName, out current!)) return true;
        if (currentVersions.TryGetValue("main", out current!)) return true;
        current = default!;
        return false;
    }

    private static (int major, int minor, int qa) Parse(string version)
    {
        var parts = version.Split('.');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }
}
