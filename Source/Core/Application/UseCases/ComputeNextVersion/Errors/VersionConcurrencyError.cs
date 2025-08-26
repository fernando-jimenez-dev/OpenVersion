using Application.Shared.Errors;
using Application.UseCases.ComputeNextVersion.Models;

namespace Application.UseCases.ComputeNextVersion.Errors;

public record VersionConcurrencyError : ApplicationError
{
    public DomainVersion Version { get; }
    public VersionConcurrencyError(DomainVersion version, Exception? exception = null)
        : base(nameof(VersionConcurrencyError), ErrorMessage(version), exception)
    {
        Version = version;
    }

    private static string ErrorMessage(DomainVersion version) =>
        $"A concurrency conflict occurred while trying to save version " +
        $"{version.ReleaseNumber} for branch {version.IdentifierName}. " +
        $"Another process may have modified the data. ";
}