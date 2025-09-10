using Application.Shared.Errors;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.Entities;
using Application.UseCases.ComputeNextVersion.Models;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;

public class VersionRepositoryTests : IClassFixture<InMemoryPerTest>
{
    private readonly OpenVersionContext _openVersionContext;
    private readonly VersionRepository _versionRepository;
    private readonly IVersionBumper _versionBumper;
    private readonly InMemoryPerTest _inMemoryPerTest;

    public VersionRepositoryTests(InMemoryPerTest inMemoryPerTest)
    {
        _inMemoryPerTest = inMemoryPerTest;
        _openVersionContext = new OpenVersionContext(inMemoryPerTest.Options);
        _versionRepository = new VersionRepository(_openVersionContext);
        _versionBumper = Substitute.For<IVersionBumper>();
    }

    #region Success Scenarios

    [Fact]
    public async Task SaveVersion_ShouldInsertNewRow_WhenKeyIsNotRegistered()
    {
        // Arrange
        await ResetDatabaseAsync();
        var mainVersion = new DomainVersion(id: 0, projectId: 1, identifierName: "main", releaseNumber: "1.0.0.0");

        // Act
        var saveResult = await _versionRepository.SaveVersion(mainVersion);
        var getAllResult = await _versionRepository.GetCurrentVersions(1);

        // Assert
        saveResult.IsSuccess.ShouldBeTrue();
        getAllResult.IsSuccess.ShouldBeTrue();

        var versionsByKey = getAllResult.Value!;
        versionsByKey.Count.ShouldBe(1);
        versionsByKey.ContainsKey("main").ShouldBeTrue();
        versionsByKey["main"]!.ReleaseNumber.ShouldBe("1.0.0.0");
    }

    [Fact]
    public async Task SaveVersion_ShouldUpdateExistingRow_WhenKeyIsAlreadyRegistered()
    {
        // Arrange
        await ResetDatabaseAsync();
        var original = new DomainVersion(0, 1, "main", "1.0.0.0", "");
        await _versionRepository.SaveVersion(original);

        var updated = new DomainVersion(original.Id, original.ProjectId, original.IdentifierName, "2.0.0.0", "lts");

        // Act
        var saveResult = await _versionRepository.SaveVersion(updated);
        var getAllResult = await _versionRepository.GetCurrentVersions(1);

        // Assert
        saveResult.IsSuccess.ShouldBeTrue();
        getAllResult.IsSuccess.ShouldBeTrue();

        var versionsByKey = getAllResult.Value!;
        versionsByKey.Count.ShouldBe(1);
        versionsByKey.ContainsKey("main").ShouldBeTrue();
        versionsByKey["main"]!.ReleaseNumber.ShouldBe("2.0.0.0");
        versionsByKey["main"]!.Meta.ShouldBe("lts");
    }

    [Fact]
    public async Task GetCurrentVersions_ShouldReturnOnlyProject1Rows()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Project 1 rows
        await _versionRepository.SaveVersion(new DomainVersion(0, 1, "main", "1.0.0.0"));
        await _versionRepository.SaveVersion(new DomainVersion(0, 1, "feature/one", "1.0.0.1"));

        // Noise: Project 2 row should not be returned
        var noiseEntity = VersionEntity.CreateFromDomain(
            new DomainVersion(999, 2, "main", "9.9.9.9")); // Use a dummy ID for the noise entity
        _openVersionContext.Versions.Add(noiseEntity);
        await _openVersionContext.SaveChangesAsync();

        // Act
        var getAllResult = await _versionRepository.GetCurrentVersions(1);

        // Assert
        getAllResult.IsSuccess.ShouldBeTrue();
        var versions = getAllResult.Value!;
        versions.Count.ShouldBe(2);
        versions.ContainsKey("main").ShouldBeTrue();
        versions.ContainsKey("feature/one").ShouldBeTrue();
        versions.ContainsKey("9.9.9.9").ShouldBeFalse(); // sanity check this isn't a key by accident
    }

    #endregion Success Scenarios

    #region Failure / Edge Scenarios

    [Fact]
    public async Task SaveVersion_ShouldReturnConcurrencyError_WhenLastUpdatedIsStale()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Create initial version
        var initialVersion = new DomainVersion(0, 1, "main", "1.0.0.0");
        await _versionRepository.SaveVersion(initialVersion);

        // Get the saved version to know its ID
        var savedVersions = await _versionRepository.GetCurrentVersions(1);
        var savedVersion = savedVersions.Value!["main"];

        // Create two repositories with different delays
        var secondOpenVersionContext = new OpenVersionContext(_inMemoryPerTest.Options);
        var slowRepository = new VersionRepository(secondOpenVersionContext, TimeSpan.FromMilliseconds(500));

        // Start the slow operation first
        var slowVersion = new DomainVersion(savedVersion.Id, 1, "main", "2.0.0.0");
        var slowTask = slowRepository.SaveVersion(slowVersion);

        // Wait a bit for the slow operation to start and reach the delay point
        await Task.Delay(100);

        // Start the fast operation (this should complete first and change the concurrency token)
        var fastVersion = new DomainVersion(savedVersion.Id, 1, "main", "1.5.0.0");
        var fastResult = await _versionRepository.SaveVersion(fastVersion);
        fastResult.IsSuccess.ShouldBeTrue(); // This should succeed

        // Now wait for the slow operation to complete - it should fail with concurrency error
        var slowResult = await slowTask;

        // Assert
        slowResult.IsSuccess.ShouldBeFalse();
        var versionConcurrencyError = slowResult.Error.ShouldBeOfType<VersionConcurrencyError>();
        versionConcurrencyError.IsExceptional().ShouldBeTrue();
        versionConcurrencyError.Exception.ShouldBeOfType<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task GetCurrentVersions_ShouldReturnEmpty_WhenTableIsEmpty()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var getAllResult = await _versionRepository.GetCurrentVersions(1);

        // Assert
        getAllResult.IsSuccess.ShouldBeTrue();
        getAllResult.Value!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveVersion_ShouldReturnRepositoryError_WhenDbContextIsDisposed()
    {
        // Arrange
        await ResetDatabaseAsync();
        _openVersionContext.Dispose();

        var versionToSaveAfterDispose = new DomainVersion(0, 1, "main", "1.0.0.0");

        // Act
        var result = await _versionRepository.SaveVersion(versionToSaveAfterDispose);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldBeOfType<ApplicationError>();
        error.Type.ShouldBe("RepositoryError");
        error.IsExceptional().ShouldBeTrue();
        error.Exception.ShouldBeOfType<ObjectDisposedException>();
    }

    #endregion Failure / Edge Scenarios

    private async Task ResetDatabaseAsync()
    {
        await _openVersionContext.Database.EnsureDeletedAsync();
        await _openVersionContext.Database.EnsureCreatedAsync();
    }
}

public sealed class InMemoryPerTest : IAsyncLifetime
{
    public DbContextOptions<OpenVersionContext> Options { get; private set; } = default!;

    public Task InitializeAsync()
    {
        var dbName = $"openversion-tests-{Guid.NewGuid():N}";
        Options = new DbContextOptionsBuilder<OpenVersionContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using var context = new OpenVersionContext(Options);
        return context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
