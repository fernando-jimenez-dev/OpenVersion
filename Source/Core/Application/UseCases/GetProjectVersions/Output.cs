using Application.UseCases.GetProjectVersions.Models;

namespace Application.UseCases.GetProjectVersions;

public partial interface IGetProjectVersionsUseCase
{
    public record Output(long ProjectId, IReadOnlyList<ProjectVersion> Versions);
}
