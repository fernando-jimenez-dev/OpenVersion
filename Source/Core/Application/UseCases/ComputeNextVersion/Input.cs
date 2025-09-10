namespace Application.UseCases.ComputeNextVersion;

public partial interface IComputeNextVersionUseCase
{
    // ProjectId is optional; defaults to 1 to preserve current behavior.
    public record Input(string BranchName, long ProjectId = 1, IReadOnlyDictionary<string, string?>? Context = null);
}
