using WebAPI.Minimal.UseCases.CheckPulse;
using WebAPI.Minimal.UseCases.ComputeNextVersion;
using WebAPI.Minimal.UseCases.GetProjectVersions;

namespace WebAPI.Minimal.StartUp;

public static class EndpointsRegistryExtensions
{
    public static void RegisterWebApiEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.AddCheckPulseEndpoint();
        routes.AddComputeNextVersionEndpoint();
        routes.AddGetProjectVersionsEndpoint();
    }

    private static IEndpointRouteBuilder AddCheckPulseEndpoint(this IEndpointRouteBuilder routes)
    {
        var groupName = "/check-pulse";
        var group = routes.MapGroup(groupName);

        group
            .MapGet("/", CheckPulseEndpoint.Execute)
            .WithName("CheckPulse")
            .WithOpenApi();

        return routes;
    }

    private static IEndpointRouteBuilder AddComputeNextVersionEndpoint(this IEndpointRouteBuilder routes)
    {
        var groupName = "/compute-next-version";
        var group = routes.MapGroup(groupName);

        group
            .MapPost("/", ComputeNextVersionEndpoint.Execute)
            .WithName("ComputeNextVersion")
            .WithOpenApi();

        return routes;
    }

    private static IEndpointRouteBuilder AddGetProjectVersionsEndpoint(this IEndpointRouteBuilder routes)
    {
        var groupName = "/projects/{projectId:long}/versions";
        var group = routes.MapGroup(groupName);

        group
            .MapGet("/", GetProjectVersionsEndpoint.Execute)
            .WithName("GetProjectVersions")
            .WithOpenApi();

        return routes;
    }
}
