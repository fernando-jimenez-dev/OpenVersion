namespace Application.UseCases.GetProjectVersions;

public partial interface IGetProjectVersionsUseCase
{
    public record Input(long ProjectId = 1);
}
