using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;

public class OpenVersionContextFactory : IDesignTimeDbContextFactory<OpenVersionContext>
{
    public OpenVersionContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenVersionContext>();

        // Fallback connection string for design-time factory
        var connectionString = "Data Source=openversion.db";
        optionsBuilder.UseSqlite(connectionString);

        return new OpenVersionContext(optionsBuilder.Options);
    }
}