using Application.UseCases.CheckPulse.Abstractions;

namespace Application.UseCases.CheckPulse.Infrastructure;

public class InMemoryCheckPulseRepository : ICheckPulseRepository
{
    public Task<string[]> RetrieveVitalReadings(CancellationToken cancellationToken = default)
        => Task.FromResult(new string[] { "All", "Good" });

    public Task SaveNewVitalCheck()
        => Task.CompletedTask;
}