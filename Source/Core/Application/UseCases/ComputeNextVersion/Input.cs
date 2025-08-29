namespace Application.UseCases.ComputeNextVersion;

public partial interface IComputeNextVersionUseCase
{
    public record Input(string BranchName, IReadOnlyDictionary<string, string?>? Context = null);
}