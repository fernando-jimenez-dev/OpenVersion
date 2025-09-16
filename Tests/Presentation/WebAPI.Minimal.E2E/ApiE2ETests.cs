using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Errors;
using Application.UseCases.ComputeNextVersion.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using WebAPI.Minimal.E2E.Support;

namespace WebAPI.Minimal.E2E;

public class ApiE2ETests
{
    [Fact]
    public async Task CheckPulse_Should_Return_200()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var response = await client.GetAsync("/check-pulse/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CheckPulseResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Pulse checked!", payload!.Message);
    }

    [Fact]
    public async Task ComputeNextVersion_Main_Major_FirstTime_Should_Return_1000()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "main", context = new Dictionary<string, string> { ["isMajor"] = "true" } };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Equal("1.0.0.0", payload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_Main_Minor_FirstTime_Should_Return_0100_WithMinorMeta()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "main" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Equal("0.1.0.0+minor", payload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_Qa_FirstTime_Should_Return_0010_WithQaMeta()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "qa" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Equal("0.0.1.0+qa", payload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_Feature_FirstTime_Should_Return_0001_WithMeta()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "feature/card-123" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Equal("0.0.0.1+feature-card-123", payload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_Fix_FirstTime_Should_Return_0001_WithFixMeta()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "fix/card-777" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Equal("0.0.0.1+fix-card-777", payload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_Feature_SecondTime_Should_Increment_LastDigit()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "feature/double" };
        var first = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstPayload = await first.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.Equal("0.0.0.1+feature-double", firstPayload!.NextVersion);

        var second = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondPayload = await second.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.Equal("0.0.0.2+feature-double", secondPayload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_UnsupportedBranch_Should_Return_400_WithMessage()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        var body = new { branchName = "random-branch" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Contains("No rule was found", payload!.NextVersion);
    }

    [Fact]
    public async Task ComputeNextVersion_UnexpectedError_Should_Return_500_StatusCode()
    {
        using var factory = new ErrorFactory(new UnexpectedErrorUseCase());
        var client = factory.CreateClient();
        var body = new { branchName = "main" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task ComputeNextVersion_ApplicationError_Should_Return_500_StatusCode()
    {
        using var factory = new ErrorFactory(new ApplicationErrorUseCase());
        var client = factory.CreateClient();
        var body = new { branchName = "main" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task ComputeNextVersion_ConcurrencyError_Should_Return_409_WithMessage()
    {
        using var factory = new ErrorFactory(new ConcurrencyErrorUseCase());
        var client = factory.CreateClient();
        var body = new { branchName = "feature/concurrent" };
        var resp = await client.PostAsJsonAsync("/compute-next-version/", body);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<ComputeNextVersionPayload>();
        Assert.NotNull(payload);
        Assert.Contains("concurrency conflict", payload!.NextVersion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ComputeNextVersion_RealConcurrency_TwoRequests_OneSucceeds_One409()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });

        // Ensure row exists so we test update concurrency (not unique insert)
        var seed = await client.PostAsJsonAsync("/compute-next-version/", new { branchName = "feature/race" });
        Assert.Equal(HttpStatusCode.OK, seed.StatusCode);

        var body = new { branchName = "feature/race" };
        var t1 = client.PostAsJsonAsync("/compute-next-version/", body);
        var t2 = client.PostAsJsonAsync("/compute-next-version/", body);

        await Task.WhenAll(t1, t2);

        var statuses = new[] { t1.Result.StatusCode, t2.Result.StatusCode };
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
    }

    [Fact]
    public async Task GetProjectVersions_Should_ReturnExistingVersionsForProject()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });

        var seedMain = await client.PostAsJsonAsync("/compute-next-version/", new { branchName = "main" });
        Assert.Equal(HttpStatusCode.OK, seedMain.StatusCode);

        var seedFeature = await client.PostAsJsonAsync("/compute-next-version/", new { branchName = "feature/e2e" });
        Assert.Equal(HttpStatusCode.OK, seedFeature.StatusCode);

        var response = await client.GetAsync("/projects/1/versions/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GetProjectVersionsPayload>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.ProjectId);
        Assert.True(payload.Versions.Length >= 2);
        Assert.Contains(payload.Versions, version => version.IdentifierName == "main");
        Assert.Contains(payload.Versions, version => version.IdentifierName == "feature/e2e");
    }

    private record CheckPulseResponse(string Message);
    private record ComputeNextVersionPayload(string NextVersion);
    private record GetProjectVersionsPayload(long ProjectId, ProjectVersionItem[] Versions);
    private record ProjectVersionItem(long Id, string IdentifierName, string ReleaseNumber, string? Meta);

    // Factories and stubs for error-path testing
    private sealed class ErrorFactory : WebApplicationFactory<WebAPI.Minimal.Program>
    {
        private readonly IComputeNextVersionUseCase _useCase;

        public ErrorFactory(IComputeNextVersionUseCase useCase) => _useCase = useCase;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.Single(d => d.ServiceType == typeof(IComputeNextVersionUseCase));
                services.Remove(descriptor);
                services.AddSingleton(_useCase);
            });
        }
    }

    private sealed class UnexpectedErrorUseCase : IComputeNextVersionUseCase
    {
        public Task<Result<IComputeNextVersionUseCase.Output>> Run(IComputeNextVersionUseCase.Input input, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IComputeNextVersionUseCase.Output>.Failure(new UnexpectedError("boom")));
    }

    private sealed class ApplicationErrorUseCase : IComputeNextVersionUseCase
    {
        public Task<Result<IComputeNextVersionUseCase.Output>> Run(IComputeNextVersionUseCase.Input input, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IComputeNextVersionUseCase.Output>.Failure(new ApplicationError("Any", "broken")));
    }

    private sealed class ConcurrencyErrorUseCase : IComputeNextVersionUseCase
    {
        public Task<Result<IComputeNextVersionUseCase.Output>> Run(IComputeNextVersionUseCase.Input input, CancellationToken cancellationToken = default)
        {
            var v = new DomainVersion(1, 1, input.BranchName, "1.0.0.1", input.BranchName.Replace('/', '-'));
            return Task.FromResult(Result<IComputeNextVersionUseCase.Output>.Failure(new VersionConcurrencyError(v)));
        }
    }
}