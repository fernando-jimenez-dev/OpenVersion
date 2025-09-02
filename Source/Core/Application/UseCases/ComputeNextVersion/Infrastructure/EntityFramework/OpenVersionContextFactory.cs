using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;

public class OpenVersionContextFactory : IDesignTimeDbContextFactory<OpenVersionContext>
{
    public OpenVersionContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenVersionContext>();

        // Load configuration from appsettings.json only (no environment variables)
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("OpenVersion");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:OpenVersion is required in appsettings.json for design-time operations.");

        optionsBuilder.UseNpgsql(connectionString);
        return new OpenVersionContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        // Attempt to locate the WebAPI.Minimal project to read its appsettings.json
        // Strategy: walk up from the current directory to find the repo root containing 'Source' folder
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        DirectoryInfo? root = null;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Source")))
            {
                root = dir;
                break;
            }
            dir = dir.Parent;
        }

        string basePath = root != null
            ? Path.Combine(root.FullName, "Source", "Presentation", "WebAPI.Minimal")
            : Directory.GetCurrentDirectory();

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            // Optional local secrets overlay (gitignored)
            .AddJsonFile("secrets.Development.json", optional: true, reloadOnChange: false);
        // Do NOT add environment variables as per requirement
        return builder.Build();
    }
}
