using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping;

public class VersionBumper : IVersionBumper
{
    private readonly IReadOnlyList<IVersionRule> _rules;

    public VersionBumper()
    {
        _rules = [.. DefaultRules().OrderBy(r => r.Priority)];
    }

    public VersionBumper(IEnumerable<IVersionRule> testRules)
    {
        _rules = [.. testRules.OrderBy(r => r.Priority)];
    }

    public async Task<Result<DomainVersion>> CalculateNextVersion(
        string branchName,
        IReadOnlyDictionary<string, DomainVersion> currentVersions,
        IReadOnlyDictionary<string, string?>? context = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var rule in _rules)
        {
            if (rule.CanApply(branchName, currentVersions, context))
            {
                return await rule.Apply(branchName, currentVersions, context, cancellationToken);
            }
        }

        return Result<DomainVersion>.Failure(new UnsupportedBranchError(branchName));
    }

    private static IReadOnlyList<IVersionRule> DefaultRules()
    {
        return
        [
            new Rules.MainMajorBumpRule(),
            new Rules.MainMinorBumpRule(),
            new Rules.QaBumpRule(),
            new Rules.FeatureBumpRule(),
            new Rules.FixBumpRule(),
            //new Rules.FallbackFromMainRule(), // Unused for now
        ];
    }
}