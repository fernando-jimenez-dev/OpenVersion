using Application.Shared.Abstractions.UseCase;

namespace Application.UseCases.GetProjectVersions;

public partial interface IGetProjectVersionsUseCase : IUseCase<IGetProjectVersionsUseCase.Input, IGetProjectVersionsUseCase.Output>
{
}
