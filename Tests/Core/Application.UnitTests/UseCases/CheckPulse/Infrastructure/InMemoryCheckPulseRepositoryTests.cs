using Application.UseCases.CheckPulse.Infrastructure;

namespace Application.UnitTests.UseCases.CheckPulse.Infrastructure;

public class InMemoryCheckPulseRepositoryTests
{
    private readonly InMemoryCheckPulseRepository repository;

    public InMemoryCheckPulseRepositoryTests()
    {
        repository = new InMemoryCheckPulseRepository();
    }

    [Fact]
    public async Task RetrieveVitalReadings_ShouldGetStoredVitals()
    {
        var vitals = await repository.RetrieveVitalReadings();

        Assert.Equal("All", vitals[0]);
        Assert.Equal("Good", vitals[1]);
    }
}