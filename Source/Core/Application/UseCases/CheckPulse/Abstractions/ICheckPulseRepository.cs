namespace Application.UseCases.CheckPulse.Abstractions;

public interface ICheckPulseRepository
{
    Task<string[]> RetrieveVitalReadings(CancellationToken cancellationToken = default);

    Task SaveNewVitalCheck();
}