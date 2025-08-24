namespace Application.UseCases.ComputeNextVersion.Abstractions;

public interface IVersionManager
{
    Task<string> GetNextVersion(string branchName, CancellationToken cancellationToken = default);
}