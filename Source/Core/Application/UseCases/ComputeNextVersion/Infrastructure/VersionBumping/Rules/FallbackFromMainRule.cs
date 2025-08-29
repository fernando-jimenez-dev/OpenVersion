using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping.Rules;

// Deprecated | Unused for now. Will test later
public class FallbackFromMainRule : IVersionRule
{
    public int Priority => 90;
    public string Name => nameof(FallbackFromMainRule);

    public bool CanApply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context)
        => !currentVersions.ContainsKey(branchName);

    public Task<Result<DomainVersion>> Apply(string branchName, IReadOnlyDictionary<string, DomainVersion> currentVersions, IReadOnlyDictionary<string, string?>? context, CancellationToken cancellationToken = default)
    {
        if (!currentVersions.TryGetValue("main", out var current))
        {
            // Nothing exists – infer starting bump by branch category
            var initial = branchName switch
            {
                "main" => new DomainVersion(0, 1, "main", "0.1.0.0", "minor"),
                var b when b.StartsWith("qa") => new DomainVersion(0, 1, branchName, "0.0.1.0", "qa"),
                var b when b.StartsWith("feature/") => new DomainVersion(0, 1, branchName, "0.0.0.1", FeatureMeta(branchName)),
                var b when b.StartsWith("fix/") => new DomainVersion(0, 1, branchName, "0.0.0.1", FixMeta(branchName)),
                _ => new DomainVersion(0, 1, branchName, "0.0.0.1", branchName)
            };
            return Task.FromResult(Result<DomainVersion>.Success(initial));
        }

        var (major, minor, qa, feature) = Parse(current.ReleaseNumber);

        if (branchName == "main")
        {
            var next = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor + 1}.0.0", "minor");
            return Task.FromResult(Result<DomainVersion>.Success(next));
        }
        if (branchName.StartsWith("qa"))
        {
            var next = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor}.{qa + 1}.0", "qa");
            return Task.FromResult(Result<DomainVersion>.Success(next));
        }
        if (branchName.StartsWith("feature/"))
        {
            var next = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor}.{qa}.{feature + 1}", FeatureMeta(branchName));
            return Task.FromResult(Result<DomainVersion>.Success(next));
        }
        if (branchName.StartsWith("fix/"))
        {
            var next = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor}.{qa}.{feature + 1}", FixMeta(branchName));
            return Task.FromResult(Result<DomainVersion>.Success(next));
        }

        // Unknown – default to last digit bump and echo meta
        var fallback = new DomainVersion(0, current.ProjectId, branchName, $"{major}.{minor}.{qa}.{feature + 1}", branchName);
        return Task.FromResult(Result<DomainVersion>.Success(fallback));
    }

    private static string FeatureMeta(string branchName) => branchName.Replace("feature/", "feature-");

    private static string FixMeta(string branchName) => branchName.Replace("fix/", "fix-");

    private static (int major, int minor, int qa, int feature) Parse(string version)
    {
        var parts = version.Split('.');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
    }
}