using Application.Shared.Abstractions.UseCase;

namespace Application.UseCases.ComputeNextVersion;

public partial interface IComputeNextVersionUseCase : IUseCase<IComputeNextVersionUseCase.Input, IComputeNextVersionUseCase.Output>
{
}