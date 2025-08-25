using Application.Shared.Errors;

namespace Application.UseCases.ComputeNextVersion.Errors;
public record UnsupportedBranchError : ApplicationError
{
    public string BranchName { get; }

    public UnsupportedBranchError(string branchName)
        : base(
            nameof(UnsupportedBranchError),
            $"No rule was found for rule {branchName}."
        )
    {
        BranchName = branchName;
    }
}