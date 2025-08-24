using WebAPI.Minimal.UseCases.CheckPulse;

namespace WebAPI.Minimal.StartUp;

public static class EndpointsRegistryExtensions
{
    public static void RegisterWebApiEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.AddCheckPulseEndpoint();
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
}