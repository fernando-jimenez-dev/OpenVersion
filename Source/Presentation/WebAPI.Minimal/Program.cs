using WebAPI.Minimal.StartUp;

var builder = WebApplication.CreateBuilder(args);

// Optional environment-specific secrets overlay (gitignored)
builder.Configuration.AddJsonFile($"secrets.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureWebApiDependencies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.RegisterWebApiEndpoints();
app.Run();