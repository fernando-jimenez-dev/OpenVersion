namespace Application.UseCases.ComputeNextVersion;

public partial interface IComputeNextVersionUseCase
{
    public record Output(string NextVersion);
}