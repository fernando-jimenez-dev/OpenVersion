using WebAPI.Minimal.StartUp;
using WebAPI.Minimal.StartUp.Security;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureWebApiDependencies(builder.Configuration);
builder.Services.Configure<PatAuthOptions>(builder.Configuration.GetSection("PatAuth"));

// Enable HTTP request/response logging (helpful for diagnosing 404s and routing)
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders;
    options.RequestHeaders.Add("X-Forwarded-For");
    options.RequestHeaders.Add("X-Forwarded-Proto");
    options.RequestHeaders.Add("X-Original-Proto");
});

var app = builder.Build();

// Startup visibility
app.Logger.LogInformation("Starting WebAPI.Minimal. Environment={Environment}", app.Environment.EnvironmentName);

// Log each request with matched endpoint and status code
app.UseHttpLogging();
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path + ctx.Request.QueryString;
    app.Logger.LogInformation(
        ">> {Method} {Path}",
        ctx.Request.Method, path);
    await next();
    var endpoint = ctx.GetEndpoint()?.DisplayName ?? "<no-endpoint>";
    app.Logger.LogInformation(
        "<< {Status} {Method} {Path} endpoint={Endpoint}",
        ctx.Response.StatusCode, ctx.Request.Method, path, endpoint);
});

// Log status code pages (e.g., 404)
app.UseStatusCodePages(async context =>
{
    var c = context.HttpContext;
    var ep = c.GetEndpoint()?.DisplayName ?? "<no-endpoint>";
    app.Logger.LogWarning("StatusCode {Status} for {Path} endpoint={Endpoint}", c.Response.StatusCode, c.Request.Path + c.Request.QueryString, ep);
    await Task.CompletedTask;
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add PAT/API-key enforcement middleware if configured
app.UseMiddleware<ApiKeyAuthMiddleware>();

// Diagnostic endpoints
app.MapGet("/_diag/env", () => Results.Json(new { Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "<unset>" }))
   .WithName("Diag.Env");
app.MapGet("/_diag/routes", (IEnumerable<EndpointDataSource> sources) =>
{
    var list = sources.SelectMany(s => s.Endpoints)
        .OfType<RouteEndpoint>()
        .Select(e => new
        {
            Route = e.RoutePattern.RawText,
            Order = e.Order,
            DisplayName = e.DisplayName,
            Methods = string.Join(",", e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? Array.Empty<string>())
        })
        .OrderBy(x => x.Route)
        .ThenBy(x => x.Order);
    return Results.Json(list);
}).WithName("Diag.Routes");

app.RegisterWebApiEndpoints();
app.Run();
